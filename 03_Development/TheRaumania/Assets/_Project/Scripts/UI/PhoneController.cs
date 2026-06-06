using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
public class PhoneController : MonoBehaviour
{
    public static PhoneController Instance;
    [Header("Screens")]
    public GameObject homeScreen;
    public List<GameObject> allApps;
    [Header("Status Bar")]
    public TextMeshProUGUI txtStatusBarTime;
    private void Awake() { Instance = this; }
    void Update()
    {
        if (txtStatusBarTime != null && HUDManager.Instance != null && HUDManager.Instance.txtTime != null)
        {
            txtStatusBarTime.text = HUDManager.Instance.txtTime.text;
        }
    }
    public void OpenApp(GameObject targetApp)
    {
        homeScreen.SetActive(false);
        foreach (var app in allApps) app.SetActive(false);
        targetApp.SetActive(true);
        Debug.Log("Mở ứng dụng: " + targetApp.name);
    }
    public void GoHome()
    {
        foreach (var app in allApps) app.SetActive(false);
        homeScreen.SetActive(true);
    }
}
