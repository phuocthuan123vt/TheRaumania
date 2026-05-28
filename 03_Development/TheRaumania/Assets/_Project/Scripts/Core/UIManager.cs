using UnityEngine;
using System.Collections.Generic;
public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    [Header("Main HUD Components")]
    public GameObject hudRoot;
    public GameObject hotbarHUD;     
    [Header("Overlay Panels")]
    public List<GameObject> overlayPanels; 
    private void Awake()
    {
        Instance = this;
        AutoMapUI();
    }

    private void Start()
    {
        AutoMapUI();
    }

    private void AutoMapUI()
    {
        Transform root = transform.root;

        if (hudRoot == null) hudRoot = RuntimeReferenceFinder.FindDeepGameObject(root, "pnl_HUD");
        if (hotbarHUD == null) hotbarHUD = RuntimeReferenceFinder.FindDeepGameObject(root, "pnl_Hotbar");

        GameObject phoneHUD = RuntimeReferenceFinder.FindDeepGameObject(root, "pnl_Phone");
        if (phoneHUD != null && phoneHUD.activeSelf)
        {
            phoneHUD.SetActive(false);
        }

        if (overlayPanels == null) overlayPanels = new List<GameObject>();

        string[] panelNames = new[]
        {
            "pnl_HUD",
            "pnl_Hotbar",
            "pnl_PauseMenu",
            "pnl_SaveDialog",
            "pnl_UpgradeDialog",
            "pnl_RecipeBook",
            "pnl_PrepMinigame",
            "pnl_Minigame_Prep",
            "pnl_SlicingMinigame",
            "pnl_Minigame_Slicing",
            "pnl_FryingMinigame",
            "pnl_Minigame_Frying",
            "pnl_Warehouse",
            "pnl_Shop"
        };

        foreach (string panelName in panelNames)
        {
            GameObject panel = RuntimeReferenceFinder.FindDeepGameObject(root, panelName);
            if (panel == null || overlayPanels.Contains(panel)) continue;
            if (panel == hudRoot || panel == hotbarHUD) continue;
            overlayPanels.Add(panel);
        }
    }
    void Update()
    {
        AutoMapUI();

        bool isAnyPanelOpen = false;
        foreach (GameObject panel in overlayPanels)
        {
            if (panel != null && panel.activeSelf)
            {
                isAnyPanelOpen = true;
                break;
            }
        }

        if (hudRoot != null) hudRoot.SetActive(!isAnyPanelOpen);
        if (hotbarHUD != null) hotbarHUD.SetActive(!isAnyPanelOpen);
    }
}
