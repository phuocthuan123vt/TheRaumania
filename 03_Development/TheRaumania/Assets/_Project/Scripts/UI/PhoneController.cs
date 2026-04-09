using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class PhoneController : MonoBehaviour
{
    public static PhoneController Instance;

    [Header("Screens")]
    public GameObject homeScreen;
    public List<GameObject> allApps; // Kéo BankApp, ServiceApp... vào đây

    [Header("Status Bar")]
    public TextMeshProUGUI txtStatusBarTime;

    private void Awake() { Instance = this; }

    void Update()
    {
        // Cập nhật giờ trên điện thoại liên tục từ TimeManager
        txtStatusBarTime.text = HUDManager.Instance.txtTime.text;
    }

    // Hàm mở một App cụ thể
    public void OpenApp(GameObject targetApp)
    {
        homeScreen.SetActive(false);
        foreach (var app in allApps) app.SetActive(false); // Tắt các app khác

        targetApp.SetActive(true);
        Debug.Log("Mở ứng dụng: " + targetApp.name);
    }

    // Hàm quay lại màn hình chính (Nút Home)
    public void GoHome()
    {
        foreach (var app in allApps) app.SetActive(false);
        homeScreen.SetActive(true);
    }
}