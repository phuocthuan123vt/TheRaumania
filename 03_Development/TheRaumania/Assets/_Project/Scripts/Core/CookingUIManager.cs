using UnityEngine;

public class CookingUIManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject pnl_RecipeBook;

    [Header("Minigame Panels")]
    public GameObject pnl_PrepMinigame;
    public GameObject pnl_SlicingMinigame;
    public GameObject pnl_FryingMinigame;

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
