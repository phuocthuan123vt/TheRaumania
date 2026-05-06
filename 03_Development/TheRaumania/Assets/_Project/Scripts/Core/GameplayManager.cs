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
        // Vừa vào game, khôi phục dữ liệu ngay từ slot 0 (Trạm chung chuyển)
        GameSaveData passedData = SaveSystem.Load(0);
        if (passedData != null)
        {
            PlayerData.SetCredit(passedData.rCredit);
            Debug.Log($"Vào map thành công! Tiền hiện có: {PlayerData.RCredit} RC");
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
        GameSaveData newDataToSave = new GameSaveData(name, PlayerData.RCredit);
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
}