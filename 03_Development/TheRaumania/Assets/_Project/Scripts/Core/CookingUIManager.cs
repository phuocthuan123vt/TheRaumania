using UnityEngine;
using UnityEngine.UI;

public class CookingUIManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject pnl_RecipeBook;

    [Header("Minigame Panels")]
    public GameObject pnl_PrepMinigame;
    public GameObject pnl_SlicingMinigame;
    public GameObject pnl_FryingMinigame;

    private void Awake()
    {
        AutoMapUI();
        AutoWireButtons();
    }

    private void Start()
    {
        AutoMapUI();
        AutoWireButtons();
    }

    private void AutoMapUI()
    {
        Transform root = transform.root;
        if (pnl_RecipeBook == null) pnl_RecipeBook = RuntimeReferenceFinder.FindDeepGameObject(root, "pnl_RecipeBook");
        if (pnl_PrepMinigame == null) pnl_PrepMinigame = RuntimeReferenceFinder.FindDeepGameObject(root, "pnl_PrepMinigame", "pnl_Minigame_Prep");
        if (pnl_SlicingMinigame == null) pnl_SlicingMinigame = RuntimeReferenceFinder.FindDeepGameObject(root, "pnl_SlicingMinigame", "pnl_Minigame_Slicing");
        if (pnl_FryingMinigame == null) pnl_FryingMinigame = RuntimeReferenceFinder.FindDeepGameObject(root, "pnl_FryingMinigame", "pnl_Minigame_Frying");
    }

    private void AutoWireButtons()
    {
        if (pnl_RecipeBook == null) return;

        WireButton("btn_Exit", HandleExitButtonClicked);
        WireButton("btn_StartCook", HandleStartCookButtonClicked);
    }

    private void WireButton(string buttonName, UnityEngine.Events.UnityAction action)
    {
        if (string.IsNullOrEmpty(buttonName) || action == null) return;

        Button button = RuntimeReferenceFinder.FindDeepComponent<Button>(pnl_RecipeBook.transform, buttonName);
        if (button == null) return;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void HandleExitButtonClicked()
    {
        CookingManager.Instance?.CloseRecipeBook();
    }

    private void HandleStartCookButtonClicked()
    {
        CookingManager.Instance?.OnClickStartCooking();
    }

    private void OnEnable()
    {
        CookingEvents.OnOpenRecipeBook += HandleOpenRecipeBook;
        CookingEvents.OnCloseRecipeBook += HandleCloseRecipeBook;
        CookingEvents.OnStartCookingSuccess += HandleStartCookingSuccess;
        CookingEvents.OnMinigameStarted += HandleMinigameStarted;
        CookingEvents.OnMinigameCompleted += HandleMinigameCompleted;
    }

    private void OnDisable()
    {
        CookingEvents.OnOpenRecipeBook -= HandleOpenRecipeBook;
        CookingEvents.OnCloseRecipeBook -= HandleCloseRecipeBook;
        CookingEvents.OnStartCookingSuccess -= HandleStartCookingSuccess;
        CookingEvents.OnMinigameStarted -= HandleMinigameStarted;
        CookingEvents.OnMinigameCompleted -= HandleMinigameCompleted;
    }

    private void HandleOpenRecipeBook()
    {
        if (pnl_RecipeBook != null)
        {
            pnl_RecipeBook.SetActive(true);
        }
    }

    private void HandleCloseRecipeBook()
    {
        if (pnl_RecipeBook != null)
        {
            pnl_RecipeBook.SetActive(false);
        }
    }

    private void HandleStartCookingSuccess()
    {
        // Khi bắt đầu nấu thành công, đóng luôn book cho rảnh rang
        if (pnl_RecipeBook != null)
        {
            pnl_RecipeBook.SetActive(false);
        }
    }

    private void HandleMinigameStarted(MinigameType type)
    {
        switch (type)
        {
            case MinigameType.Prep: if(pnl_PrepMinigame != null) pnl_PrepMinigame.SetActive(true); break;
            case MinigameType.Slicing: if(pnl_SlicingMinigame != null) pnl_SlicingMinigame.SetActive(true); break;
            case MinigameType.Frying: if(pnl_FryingMinigame != null) pnl_FryingMinigame.SetActive(true); break;
        }
    }

    private void HandleMinigameCompleted(MinigameType type)
    {
        // Các Panel sẽ tự động tắt khi minigame đó hoàn thành
        switch (type)
        {
            case MinigameType.Prep: if(pnl_PrepMinigame != null) pnl_PrepMinigame.SetActive(false); break;
            case MinigameType.Slicing: if(pnl_SlicingMinigame != null) pnl_SlicingMinigame.SetActive(false); break;
            case MinigameType.Frying: if(pnl_FryingMinigame != null) pnl_FryingMinigame.SetActive(false); break;
        }
    }
}
