using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameplayManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject pnlPauseMenu;
    public GameObject pnlSaveDialog;

    [Header("UI Save")]
    public TMP_InputField inputSaveName;
    public TextMeshProUGUI[] slotTexts;

    private bool _isPaused = false;

    private void Start()
    {
        // Defensive reset to avoid being stuck after entering the game
        Time.timeScale = 1f;
        EnsurePlayerMovementEnabled();

        // Vừa vào game, khôi phục dữ liệu ngay từ slot 0 (Trạm chung chuyển)
        GameSaveData passedData = SaveSystem.Load(0);
        if (passedData != null)
        {
            PlayerData.SetCredit(passedData.rCredit);
            PlayerData.foodQualityScore = passedData.foodQualityScore;
            PlayerData.hygieneScore = passedData.hygieneScore;
            PlayerData.decorationScore = passedData.decorationScore;
            PlayerData.satisfactionHistory = new System.Collections.Generic.Queue<float>(passedData.satisfactionHistory);

            RestorePlayerPosition(passedData);
            RestoreWarehouse(passedData);
            RestoreHotbar(passedData);
            
            Debug.Log($"Vào map thành công! Tiền hiện có: {PlayerData.RCredit} RC");
        }
    }

    private void EnsurePlayerMovementEnabled()
    {
        var player = FindObjectOfType<PlayerMovement>(true);
        if (player == null) return;

        if (!player.enabled)
        {
            player.enabled = true;
        }

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.simulated = true;
            rb.velocity = Vector2.zero;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (pnlSaveDialog.activeSelf) pnlSaveDialog.SetActive(false); // Nếu đang mở màng lưu thì đóng lại đi
        
        _isPaused = !_isPaused;
        pnlPauseMenu.SetActive(_isPaused);
        Time.timeScale = _isPaused ? 0f : 1f; // Dừng/Chạy thời gian
    }

    public void Btn_OpenSaveDialog()
    {
        pnlSaveDialog.SetActive(true);
        RefreshSlotsUI();
    }

    public void Btn_CloseSaveDialog() => pnlSaveDialog.SetActive(false);

    // Khi người chơi ấn chọn Slot lưu
    public void OnSaveSlotClicked(int slotIndex)
    {
        string name = inputSaveName.text;
        if (string.IsNullOrEmpty(name)) name = $"SaveData_{slotIndex}";

        // Tóm lấy tài sản hiện tại
        GameSaveData newDataToSave = new GameSaveData(name, PlayerData.RCredit)
        {
            foodQualityScore = PlayerData.foodQualityScore,
            hygieneScore = PlayerData.hygieneScore,
            decorationScore = PlayerData.decorationScore,
            satisfactionHistory = new System.Collections.Generic.List<float>(PlayerData.satisfactionHistory)
        };

        FillPlayerPosition(newDataToSave);
        FillWarehouse(newDataToSave);
        FillUpgradeData(newDataToSave);
        FillHotbar(newDataToSave);
        SaveSystem.Save(slotIndex, newDataToSave);

        inputSaveName.text = ""; // Xóa input
        RefreshSlotsUI();        // Load lại chữ
    }

    public void Btn_QuitToMenu()
    {
        Time.timeScale = 1f; // Chắc chắn xả Pause trước khi quay về Menu
        SceneManager.LoadScene("scn_MainMenu");
    }

    private void RefreshSlotsUI()
    {
        for (int i = 0; i < slotTexts.Length; i++)
        {
            int slot = i + 1;
            GameSaveData currentSlot = SaveSystem.Load(slot);
            slotTexts[i].text = currentSlot != null ? $"Slot {slot}: {currentSlot.saveFileName}" : $"Slot {slot}: --- Trống ---";
        }
    }

    private void FillPlayerPosition(GameSaveData data)
    {
        var player = FindObjectOfType<PlayerMovement>(true);
        if (player != null)
        {
            Vector3 pos = player.transform.position;
            data.playerPosX = pos.x;
            data.playerPosY = pos.y;
            data.playerPosZ = pos.z;
            data.hasPlayerPosition = true;
        }
        data.sceneName = SceneManager.GetActiveScene().name;
    }

    private void FillWarehouse(GameSaveData data)
    {
        if (WarehouseManager.Instance == null) return;

        data.coldStorage = BuildItemDataList(WarehouseManager.Instance.coldStorage);
        data.dryStorage = BuildItemDataList(WarehouseManager.Instance.dryStorage);
    }

    private void FillHotbar(GameSaveData data)
    {
        if (HotbarManager.Instance == null) return;

        data.hotbarItems = BuildItemDataList(HotbarManager.Instance.items);
    }

    private void RestorePlayerPosition(GameSaveData data)
    {
        var player = FindObjectOfType<PlayerMovement>(true);
        if (player == null) return;

        if (!data.hasPlayerPosition)
        {
            return;
        }

        if (!string.IsNullOrEmpty(data.sceneName) && SceneManager.GetActiveScene().name != data.sceneName)
        {
            // Scene mismatch: position will be applied after loading the correct scene
            return;
        }

        player.transform.position = new Vector3(data.playerPosX, data.playerPosY, data.playerPosZ);
    }

    private void RestoreWarehouse(GameSaveData data)
    {
        if (WarehouseManager.Instance == null) return;

        WarehouseManager.Instance.coldStorage = BuildStoredItems(data.coldStorage);
        WarehouseManager.Instance.dryStorage = BuildStoredItems(data.dryStorage);
    }

    private void RestoreHotbar(GameSaveData data)
    {
        if (HotbarManager.Instance == null) return;

        HotbarManager.Instance.items = BuildStoredItems(data.hotbarItems);
        HotbarManager.Instance.RefreshUI();
        
        // Restore upgrade system if present
        RestoreUpgradeData(data);
    }

    private void FillUpgradeData(GameSaveData data)
    {
        if (UpgradeManager.Instance == null) return;
        data.highestUnlockedRestaurantLevel = UpgradeManager.Instance.highestUnlockedLevel;
        data.upgradeCurrentPrices = new System.Collections.Generic.List<int>(UpgradeManager.Instance.upgradeCurrentPrices);
        data.upgradeBargainAllowed = new System.Collections.Generic.List<bool>(UpgradeManager.Instance.upgradeBargainAllowed);
    }

    private void RestoreUpgradeData(GameSaveData data)
    {
        if (UpgradeManager.Instance == null) return;
        UpgradeManager.Instance.RestoreFromSave(data);
    }

    private System.Collections.Generic.List<StoredItemData> BuildItemDataList(System.Collections.Generic.List<StoredItem> source)
    {
        var list = new System.Collections.Generic.List<StoredItemData>();
        if (source == null) return list;

        foreach (var item in source)
        {
            if (item == null || item.itemData == null) continue;
            list.Add(new StoredItemData(item.itemData.id, item.quantity, item.currentFreshness));
        }
        return list;
    }

    private System.Collections.Generic.List<StoredItem> BuildStoredItems(System.Collections.Generic.List<StoredItemData> source)
    {
        var list = new System.Collections.Generic.List<StoredItem>();
        if (source == null) return list;

        foreach (var data in source)
        {
            BaseItemSO item = ResolveItemById(data.itemId);
            if (item == null) continue;
            var stored = new StoredItem(item, data.quantity) { currentFreshness = data.freshness };
            stored.quantity = data.quantity;
            list.Add(stored);
        }
        return list;
    }

    private BaseItemSO ResolveItemById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        if (ShopManager.Instance != null && ShopManager.Instance.allItems != null)
        {
            var found = ShopManager.Instance.allItems.Find(x => x != null && x.id == id);
            if (found != null) return found;
        }

        var all = Resources.FindObjectsOfTypeAll<BaseItemSO>();
        foreach (var item in all)
        {
            if (item != null && item.id == id) return item;
        }

        return null;
    }
}