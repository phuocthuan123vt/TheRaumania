using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject pnlMainMenu;
    public GameObject pnlLoadDialog; // Bảng chọn Slot

    [Header("Slots")]
    public TextMeshProUGUI[] slotTexts; // 10 Text của 10 nút Slot

    [Header("Intro Video")]
    public IntroVideoController introController;

    [Header("Audio")]
    public AudioSource mainMenuBgm;

    private void Awake()
    {
        AutoMapUI();
    }

    private void Start()
    {
        AutoMapUI();
    }

    private void AutoMapUI()
    {
        Transform root = transform.root;

        if (pnlMainMenu == null) pnlMainMenu = RuntimeReferenceFinder.FindDeepGameObject(root, "pnl_MainMenu");
        if (pnlLoadDialog == null) pnlLoadDialog = RuntimeReferenceFinder.FindDeepGameObject(root, "pnl_LoadDialog");

        if ((slotTexts == null || slotTexts.Length == 0 || System.Array.Exists(slotTexts, slot => slot == null)) && pnlLoadDialog != null)
        {
            var slotButtons = RuntimeReferenceFinder.FindChildrenMatching(
                pnlLoadDialog.transform,
                t => t.name.StartsWith("btn_slot_") && t.GetComponent<UnityEngine.UI.Button>() != null);

            slotButtons.Sort((a, b) => a.GetSiblingIndex().CompareTo(b.GetSiblingIndex()));

            int takeCount = Mathf.Min(10, slotButtons.Count);
            slotTexts = new TextMeshProUGUI[takeCount];

            for (int i = 0; i < takeCount; i++)
            {
                slotTexts[i] = slotButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }

        if (introController == null) introController = GetComponentInChildren<IntroVideoController>(true);
        if (mainMenuBgm == null) mainMenuBgm = GetComponentInChildren<AudioSource>(true);
    }

    // Nút New Game
    public void Btn_NewGame()
    {
        // Khởi tạo data mới cứng
        GameSaveData newData = new GameSaveData("AutoSave_NewGame", 100000);
        newData.sceneName = "scn_Village";
        newData.hasPlayerPosition = false;
        newData.dayCount = 1;
        newData.hourOfDay = 5;
        newData.minuteOfHour = 0;
        newData.hasTimeState = true;
        SaveSystem.Save(0, newData); // Lưu tạm vào slot 0 để qua scene kia đọc lại

        if (mainMenuBgm != null) mainMenuBgm.Stop();

        if (introController != null)
        {
            introController.PlayIntro(() => LoadGameplayScene("scn_Village"));
        }
        else
        {
            LoadGameplayScene("scn_Village");
        }
    }

    // Mở bảng Load Game
    public void Btn_OpenLoadDialog()
    {
        pnlLoadDialog.SetActive(true);
        RefreshSlotsUI();
    }

    public void Btn_CloseLoadDialog() => pnlLoadDialog.SetActive(false);

    // Xử lý khi user bấm vào 1 Slot (1 -> 10)
    public void OnSlotClicked(int slotIndex)
    {
        GameSaveData data = SaveSystem.Load(slotIndex);
        if (data != null)
        {
            // Bấm nhầm Slot không trống -> Load tạm vào slot 0 để sang scene kia nhận
            SaveSystem.Save(0, data); 
            if (mainMenuBgm != null) mainMenuBgm.Stop();
            LoadGameplayScene(null);
        }
        else
        {
            Debug.LogWarning("Slot này chưa có dữ liệu!");
        }
    }

    public void Btn_Quit() => Application.Quit();

    private void RefreshSlotsUI()
    {
        if (slotTexts == null || slotTexts.Length == 0)
        {
            AutoMapUI();
        }

        if (slotTexts == null || slotTexts.Length == 0) return;

        for (int i = 0; i < slotTexts.Length; i++)
        {
            int slot = i + 1;
            GameSaveData currentSlot = SaveSystem.Load(slot);
            if (slotTexts[i] != null)
            {
                slotTexts[i].text = currentSlot != null ? $"Slot {slot}: {currentSlot.saveFileName}" : $"Slot {slot}: --- Trống ---";
            }
        }
    }

    private void LoadGameplayScene(string overrideScene)
    {
        if (!string.IsNullOrEmpty(overrideScene))
        {
            SceneManager.LoadScene(overrideScene);
            return;
        }

        GameSaveData data = SaveSystem.Load(0);
        string sceneName = (data != null && !string.IsNullOrEmpty(data.sceneName)) ? data.sceneName : "scn_Village";
        SceneManager.LoadScene(sceneName);
    }
}