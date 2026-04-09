using UnityEngine;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Main HUD Components")]
    public GameObject phoneHUD;      // HUD điện thoại mới
    public GameObject hotbarHUD;     // Thanh công cụ 10 ô

    [Header("Overlay Panels")]
    public List<GameObject> overlayPanels; // Danh sách các bảng (Shop, Warehouse, Recipe, Minigames)

    private void Awake() { Instance = this; }

    void Update()
    {
        // Kiểm tra xem có bất kỳ bảng Menu nào đang mở không
        bool isAnyPanelOpen = false;
        foreach (GameObject panel in overlayPanels)
        {
            if (panel != null && panel.activeSelf)
            {
                isAnyPanelOpen = true;
                break;
            }
        }

        // Nếu có Menu mở -> Ẩn HUD chính. Nếu không -> Hiện HUD chính.
        phoneHUD.SetActive(!isAnyPanelOpen);
        hotbarHUD.SetActive(!isAnyPanelOpen);
    }
}