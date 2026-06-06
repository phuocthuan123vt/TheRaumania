using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

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

        if (pnlLoadDialog != null)
        {
            var slotButtons = RuntimeReferenceFinder.FindChildrenMatching(
                pnlLoadDialog.transform,
                t => t.name.StartsWith("btn_slot_") && t.GetComponent<UnityEngine.UI.Button>() != null);

            slotButtons.Sort((a, b) => a.GetSiblingIndex().CompareTo(b.GetSiblingIndex()));

            int takeCount = Mathf.Min(10, slotButtons.Count);
            if (slotTexts == null || slotTexts.Length != takeCount || System.Array.Exists(slotTexts, slot => slot == null))
            {
                slotTexts = new TextMeshProUGUI[takeCount];
            }

            for (int i = 0; i < takeCount; i++)
            {
                slotTexts[i] = slotButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                
                UnityEngine.UI.Button btn = slotButtons[i].GetComponent<UnityEngine.UI.Button>();
                if (btn != null)
                {
                    int slotIndex = i + 1;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnSlotClicked(slotIndex));
                }
            }
        }

        if (introController == null) introController = GetComponentInChildren<IntroVideoController>(true);
        if (mainMenuBgm == null) mainMenuBgm = GetComponentInChildren<AudioSource>(true);

        AudioSettingsManager.EnsureInstance();

        WireSettingsButton();
    }

    // Nút New Game
    public void Btn_NewGame()
    {
        // Defensive: clear any lingering gameplay pause UI/state before starting a new run
        ForceResetGameplayPauseState();

        // Force default spawn for a true New Game start in village
        PersistentGameManager.TargetSpawnPointName = "Village_EntryPoint_FromHome";

        // Khởi tạo data mới cứng
        GameSaveData newData = new GameSaveData("AutoSave_NewGame", 500);
        newData.sceneName = "scn_Village";
        newData.hasPlayerPosition = false;
        newData.dayCount = 1;
        newData.hourOfDay = 5;
        newData.minuteOfHour = 0;
        newData.hasTimeState = true;
        newData.foodQualityScore = 0f;
        newData.hygieneScore = 10f;
        newData.decorationScore = 3f;
        newData.satisfactionHistory.Clear();
        SaveSystem.Save(0, newData); // Lưu tạm vào slot 0 để qua scene kia đọc lại
        Debug.Log("MainMenuManager: Btn_NewGame - saved new game to slot 0.");

        // Stop any main menu BGM or other AudioManager instances to avoid overlap
        if (mainMenuBgm != null) mainMenuBgm.Stop();
        var sceneAudio = GameObject.Find("AudioManager");
        if (sceneAudio != null)
        {
            var src = sceneAudio.GetComponent<AudioSource>();
            if (src != null && src.isPlaying) src.Stop();
        }

        // Keep intro overlay lifecycle inside IntroVideoController to avoid destroy-in-frame race

        if (introController != null)
        {
            if (pnlMainMenu != null) pnlMainMenu.SetActive(false);
            Debug.Log("MainMenuManager: Btn_NewGame - starting intro.");
            introController.PlayIntro(() => {
                Debug.Log("MainMenuManager: Intro finished, loading gameplay scene scn_Village.");
                LoadGameplayScene("scn_Village");
            });
        }
        else
        {
            Debug.Log("MainMenuManager: Btn_NewGame - no intro controller, loading gameplay scene immediately.");
            LoadGameplayScene("scn_Village");
        }
    }

    private void ForceResetGameplayPauseState()
    {
        var gameplay = FindObjectOfType<GameplayManager>(true);
        if (gameplay != null)
        {
            gameplay.ForceUnpause();
        }
        else
        {
            Time.timeScale = 1f;

            var pausePanel = GameObject.Find("pnl_PauseMenu");
            if (pausePanel != null && pausePanel.activeSelf) pausePanel.SetActive(false);

            var saveDialog = GameObject.Find("pnl_SaveDialog");
            if (saveDialog != null && saveDialog.activeSelf) saveDialog.SetActive(false);
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

    public void Btn_OpenSettings()
    {
        AudioSettingsManager.EnsureInstance().OpenSettings();
    }

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
            Debug.Log($"MainMenuManager: LoadGameplayScene requested override '{overrideScene}'.");
            SceneManager.LoadScene(overrideScene);
            return;
        }

        GameSaveData data = SaveSystem.Load(0);
        string sceneName = (data != null && !string.IsNullOrEmpty(data.sceneName)) ? data.sceneName : "scn_Village";
        Debug.Log($"MainMenuManager: LoadGameplayScene loading '{sceneName}' from save slot 0.");
        SceneManager.LoadScene(sceneName);
    }

    private void WireSettingsButton()
    {
        if (pnlMainMenu == null) return;

        var settingsButton = RuntimeReferenceFinder.FindDeepComponent<Button>(pnlMainMenu.transform, "btn_settings", "btn_Settings", "Settings");
        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(Btn_OpenSettings);
        }
    }
}