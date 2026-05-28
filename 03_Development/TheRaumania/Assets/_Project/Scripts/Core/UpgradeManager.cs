using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;

    [Header("Upgrade Data")]
    [Tooltip("Highest unlocked restaurant level (1..3)")]
    public int highestUnlockedLevel = 1;

    [Tooltip("Base/current prices for upgrading to level index. Use index 1..3; index 0 unused")]
    public List<int> upgradeCurrentPrices = new List<int>() { 0, 0, 2000, 5000 };

    [Tooltip("Whether bargaining is allowed for target level index")]
    public List<bool> upgradeBargainAllowed = new List<bool>() { false, false, true, true };

    [Header("UI (assign in inspector or map at runtime)")]
    public GameObject pnlUpgradeDialog;
    public TextMeshProUGUI txtUpgradeMessage;
    public TMP_InputField inputOfferAmount;
    public Button btnAgree;
    public Button btnBargain;
    public Button btnCancel;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.transform.root.gameObject);
            AutoMapUI();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        AutoMapUI();
        if (pnlUpgradeDialog != null) pnlUpgradeDialog.SetActive(false);
        WireButtons();
    }

    private void AutoMapUI()
    {
        Transform root = transform.root;

        if (pnlUpgradeDialog == null) pnlUpgradeDialog = RuntimeReferenceFinder.FindDeepGameObject(root, "pnl_UpgradeDialog");

        if (pnlUpgradeDialog != null)
        {
            if (txtUpgradeMessage == null) txtUpgradeMessage = pnlUpgradeDialog.GetComponentInChildren<TextMeshProUGUI>(true);
            if (inputOfferAmount == null) inputOfferAmount = pnlUpgradeDialog.GetComponentInChildren<TMP_InputField>(true);

            if (btnAgree == null)
            {
                btnAgree = RuntimeReferenceFinder.FindDeepComponent<Button>(pnlUpgradeDialog.transform, "btnAgree");
                if (btnAgree == null) btnAgree = FindButtonByText(pnlUpgradeDialog.transform, "Đồng ý");
            }

            if (btnBargain == null)
            {
                btnBargain = RuntimeReferenceFinder.FindDeepComponent<Button>(pnlUpgradeDialog.transform, "btnBargain");
                if (btnBargain == null) btnBargain = FindButtonByText(pnlUpgradeDialog.transform, "Trả giá");
            }

            if (btnCancel == null)
            {
                btnCancel = RuntimeReferenceFinder.FindDeepComponent<Button>(pnlUpgradeDialog.transform, "btnCancel");
                if (btnCancel == null) btnCancel = FindButtonByText(pnlUpgradeDialog.transform, "Hủy");
            }
        }
    }

    private Button FindButtonByText(Transform root, string label)
    {
        var buttons = root.GetComponentsInChildren<Button>(true);
        foreach (var button in buttons)
        {
            var text = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null && text.text != null && text.text.Contains(label)) return button;
        }
        return null;
    }

    private void WireButtons()
    {
        if (btnAgree != null) btnAgree.onClick.AddListener(OnAgreeClicked);
        if (btnBargain != null) btnBargain.onClick.AddListener(OnBargainClicked);
        if (btnCancel != null) btnCancel.onClick.AddListener(CloseDialog);
    }

    public int GetNextLevelForCurrent()
    {
        int next = Mathf.Clamp(highestUnlockedLevel + 1, 1, 3);
        if (next <= highestUnlockedLevel) return -1;
        return next;
    }

    public int GetPriceForLevel(int targetLevel)
    {
        if (upgradeCurrentPrices == null) return 0;
        if (targetLevel < 0 || targetLevel >= upgradeCurrentPrices.Count) return 0;
        return upgradeCurrentPrices[targetLevel];
    }

    public bool IsBargainAllowed(int targetLevel)
    {
        if (upgradeBargainAllowed == null) return false;
        if (targetLevel < 0 || targetLevel >= upgradeBargainAllowed.Count) return false;
        return upgradeBargainAllowed[targetLevel];
    }

    public void ShowUpgradeDialog()
    {
        int next = GetNextLevelForCurrent();
        if (next == -1)
        {
            // Nothing to upgrade
            if (txtUpgradeMessage != null) txtUpgradeMessage.text = "Nhà hàng đã đạt cấp tối đa.";
            if (pnlUpgradeDialog != null) pnlUpgradeDialog.SetActive(true);
            return;
        }

        int price = GetPriceForLevel(next);
        bool canBargain = IsBargainAllowed(next);
        if (txtUpgradeMessage != null) txtUpgradeMessage.text = $"Nâng nhà lên cấp {next} sẽ tốn {price} RC. Bạn có muốn nâng cấp?";
        if (inputOfferAmount != null) inputOfferAmount.text = price.ToString();
        if (btnBargain != null) btnBargain.gameObject.SetActive(canBargain);
        if (pnlUpgradeDialog != null) pnlUpgradeDialog.SetActive(true);
    }

    public void CloseDialog()
    {
        if (pnlUpgradeDialog != null) pnlUpgradeDialog.SetActive(false);
    }

    private void OnAgreeClicked()
    {
        int next = GetNextLevelForCurrent();
        if (next == -1) return;
        int price = GetPriceForLevel(next);
        if (!PlayerData.SpendCredit(price))
        {
            if (txtUpgradeMessage != null) txtUpgradeMessage.text = "Không đủ tiền để nâng cấp.";
            return;
        }
        ApplyUpgrade(next);
        if (txtUpgradeMessage != null) txtUpgradeMessage.text = $"Nâng cấp lên cấp {next} thành công!";
        CloseDialog();
    }

    private void OnBargainClicked()
    {
        int next = GetNextLevelForCurrent();
        if (next == -1) return;
        if (!IsBargainAllowed(next))
        {
            if (txtUpgradeMessage != null) txtUpgradeMessage.text = "Không thể trả giá cho cấp này nữa.";
            return;
        }

        int basePrice = GetPriceForLevel(next);
        int offer = basePrice;
        if (inputOfferAmount != null && !int.TryParse(inputOfferAmount.text, out offer))
        {
            offer = basePrice;
        }

        // Success chance proportional to offer/basePrice, capped
        float chance = Mathf.Clamp01((float)offer / (float)basePrice);
        float roll = Random.value; // 0..1
        bool success = roll <= chance;

        if (success)
        {
            // charge the offered amount (could be less than base)
            if (!PlayerData.SpendCredit(offer))
            {
                if (txtUpgradeMessage != null) txtUpgradeMessage.text = "Bạn không có đủ tiền để trả giá.";
                return;
            }
            ApplyUpgrade(next);
            if (txtUpgradeMessage != null) txtUpgradeMessage.text = $"Trả giá thành công! Bạn đã nâng cấp lên cấp {next}.";
            CloseDialog();
            return;
        }
        else
        {
            // Failure: double the base price and disable further bargaining for this level
            int newPrice = Mathf.Min(basePrice * 2, int.MaxValue);
            if (upgradeCurrentPrices == null) upgradeCurrentPrices = new List<int>() { 0, 0, 2000, 5000 };
            if (next >= 0 && next < upgradeCurrentPrices.Count) upgradeCurrentPrices[next] = newPrice;
            if (upgradeBargainAllowed == null) upgradeBargainAllowed = new List<bool>() { false, false, true, true };
            if (next >= 0 && next < upgradeBargainAllowed.Count) upgradeBargainAllowed[next] = false;

            if (txtUpgradeMessage != null) txtUpgradeMessage.text = $"Trả giá thất bại. Giá nâng cấp cho cấp {next} đã tăng lên {newPrice} RC. Không thể trả giá nữa.";
            if (btnBargain != null) btnBargain.gameObject.SetActive(false);
            return;
        }
    }

    private void ApplyUpgrade(int targetLevel)
    {
        if (targetLevel <= highestUnlockedLevel) return;
        highestUnlockedLevel = Mathf.Clamp(targetLevel, 1, 3);
        // Persist changes immediately via SaveSystem (slot 0 auto-save)
        GameSaveData data = SaveSystem.Load(0) ?? new GameSaveData("autosave", PlayerData.RCredit);
        data.highestUnlockedRestaurantLevel = highestUnlockedLevel;
        data.upgradeCurrentPrices = new List<int>(upgradeCurrentPrices);
        data.upgradeBargainAllowed = new List<bool>(upgradeBargainAllowed);
        SaveSystem.Save(0, data);
    }

    // Helper for restoring from save
    public void RestoreFromSave(GameSaveData data)
    {
        if (data == null) return;
        highestUnlockedLevel = Mathf.Clamp(data.highestUnlockedRestaurantLevel, 1, 3);
        if (data.upgradeCurrentPrices != null && data.upgradeCurrentPrices.Count > 0)
            upgradeCurrentPrices = new List<int>(data.upgradeCurrentPrices);
        if (data.upgradeBargainAllowed != null && data.upgradeBargainAllowed.Count > 0)
            upgradeBargainAllowed = new List<bool>(data.upgradeBargainAllowed);
    }
}
