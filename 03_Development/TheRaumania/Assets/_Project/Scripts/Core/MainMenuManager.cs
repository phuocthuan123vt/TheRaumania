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

    // Nút New Game
    public void Btn_NewGame()
    {
        // Khởi tạo data mới cứng
        GameSaveData newData = new GameSaveData("AutoSave_NewGame", 1000);
        SaveSystem.Save(0, newData); // Lưu tạm vào slot 0 để qua scene kia đọc lại
        
        LoadGameplayScene();
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
            LoadGameplayScene();
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

    private void LoadGameplayScene()
    {
        SceneManager.LoadScene("scn_Village");
    }
}