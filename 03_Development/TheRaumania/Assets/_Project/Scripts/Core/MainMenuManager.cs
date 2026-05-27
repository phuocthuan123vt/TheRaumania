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

    // Nút New Game
    public void Btn_NewGame()
    {
        // Khởi tạo data mới cứng
        GameSaveData newData = new GameSaveData("AutoSave_NewGame", 100000);
        newData.sceneName = "";
        newData.hasPlayerPosition = false;
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
        for (int i = 0; i < slotTexts.Length; i++)
        {
            int slot = i + 1;
            GameSaveData currentSlot = SaveSystem.Load(slot);
            slotTexts[i].text = currentSlot != null ? $"Slot {slot}: {currentSlot.saveFileName}" : $"Slot {slot}: --- Trống ---";
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