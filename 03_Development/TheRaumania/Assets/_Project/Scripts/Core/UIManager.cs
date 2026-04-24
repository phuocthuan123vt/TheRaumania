using UnityEngine;
using System.Collections.Generic;
public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    [Header("Main HUD Components")]
    public GameObject phoneHUD;     
    public GameObject hotbarHUD;     
    [Header("Overlay Panels")]
    public List<GameObject> overlayPanels; 
    private void Awake() { Instance = this; }
    void Update()
    {
        bool isAnyPanelOpen = false;
        foreach (GameObject panel in overlayPanels)
        {
            if (panel != null && panel.activeSelf)
            {
                isAnyPanelOpen = true;
                break;
            }
        }
        phoneHUD.SetActive(!isAnyPanelOpen);
        hotbarHUD.SetActive(!isAnyPanelOpen);
    }
}
