using System;
using UnityEngine;

public static class CookingEvents
{
    // Cầm trịch các sự kiện liên quan đến nấu ăn
    public static Action OnOpenRecipeBook;
    public static Action OnCloseRecipeBook;
    public static Action<RecipeSO> OnRecipeSelected;
    public static Action OnStartCookingSuccess;
    public static Action OnStartCookingFailed_NoRecipe;
    public static Action OnStartCookingFailed_NoIngredients;
    public static Action<RecipeSO, float, float> OnDishFinalized; // Recipe, finalScore, stars

    // --- SỰ KIỆN MINIGAME ---
    public static Action<MinigameType> OnMinigameStarted;
    public static Action<MinigameType> OnMinigameCompleted;
}

public enum MinigameType
{
    Prep,
    Slicing,
    Frying
}
