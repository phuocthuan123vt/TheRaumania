using UnityEngine;
using System.Collections.Generic;
public class CookingManager : MonoBehaviour
{
    public static CookingManager Instance;
    [Header("Minigame Panels")]
    public PrepMinigame mg1_Prep;
    public SlicingMinigame mg2_Slicing;
    public FryingMinigame mg3_Frying;
    private RecipeSO _selectedRecipe;
    private float _mg1Score, _mg2Score, _mg3Score;
    private float _avgFreshness;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Ensure we call DontDestroyOnLoad on the root GameObject to avoid editor warning
            DontDestroyOnLoad(this.transform.root.gameObject);
            AutoMapMinigames();
        }
        else if (Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
    }

    private void Start()
    {
        AutoMapMinigames();
    }

    private void AutoMapMinigames()
    {
        Transform root = transform.root;
        if (mg1_Prep == null) mg1_Prep = RuntimeReferenceFinder.FindDeepComponent<PrepMinigame>(root, "pnl_PrepMinigame", "pnl_Minigame_Prep");
        if (mg2_Slicing == null) mg2_Slicing = RuntimeReferenceFinder.FindDeepComponent<SlicingMinigame>(root, "pnl_SlicingMinigame", "pnl_Minigame_Slicing");
        if (mg3_Frying == null) mg3_Frying = RuntimeReferenceFinder.FindDeepComponent<FryingMinigame>(root, "pnl_FryingMinigame", "pnl_Minigame_Frying");

        if (mg1_Prep == null)
        {
            GameObject found = RuntimeReferenceFinder.FindGameObjectInLoadedScenes("pnl_PrepMinigame") ?? RuntimeReferenceFinder.FindGameObjectInLoadedScenes("pnl_Minigame_Prep");
            if (found != null) mg1_Prep = found.GetComponentInChildren<PrepMinigame>(true);
        }

        if (mg1_Prep == null)
        {
            mg1_Prep = RuntimeReferenceFinder.FindComponentInLoadedScenes<PrepMinigame>();
        }

        if (mg2_Slicing == null)
        {
            GameObject found = RuntimeReferenceFinder.FindGameObjectInLoadedScenes("pnl_SlicingMinigame") ?? RuntimeReferenceFinder.FindGameObjectInLoadedScenes("pnl_Minigame_Slicing");
            if (found != null) mg2_Slicing = found.GetComponentInChildren<SlicingMinigame>(true);
        }

        if (mg2_Slicing == null)
        {
            mg2_Slicing = RuntimeReferenceFinder.FindComponentInLoadedScenes<SlicingMinigame>();
        }

        if (mg3_Frying == null)
        {
            GameObject found = RuntimeReferenceFinder.FindGameObjectInLoadedScenes("pnl_FryingMinigame") ?? RuntimeReferenceFinder.FindGameObjectInLoadedScenes("pnl_Minigame_Frying");
            if (found != null) mg3_Frying = found.GetComponentInChildren<FryingMinigame>(true);
        }

        if (mg3_Frying == null)
        {
            mg3_Frying = RuntimeReferenceFinder.FindComponentInLoadedScenes<FryingMinigame>();
        }
    }
    public void OpenRecipeBook()
    {
        _selectedRecipe = null;
        CookingEvents.OnOpenRecipeBook?.Invoke();
    }
    public void SelectRecipe(RecipeSO recipe)
    {
        _selectedRecipe = recipe;
        CookingEvents.OnRecipeSelected?.Invoke(recipe);
        Debug.Log("<color=cyan>Đã chọn món: </color>" + recipe.dishName);
    }
    public void OnClickStartCooking()
    {
        if (_selectedRecipe == null)
        {
            CookingEvents.OnStartCookingFailed_NoRecipe?.Invoke();
            Debug.Log("<color=red>Chưa chọn món ông ơi!</color>");
            return;
        }
        if (CanCook(_selectedRecipe))
        {
            CookingEvents.OnStartCookingSuccess?.Invoke();
            StartStep1();
        }
        else
        {
            CookingEvents.OnStartCookingFailed_NoIngredients?.Invoke();
            Debug.Log("<color=orange>Alex ơi, thiếu nguyên liệu rồi!</color>");
        }
    }
    private bool CanCook(RecipeSO recipe)
    {
        List<StoredItem> playerInv = new List<StoredItem>(PlayerInventory.Instance.carriedItems);
        foreach (var req in recipe.ingredientsRequired)
        {
            StoredItem match = playerInv.Find(x => x.itemData.id == req.id);
            if (match != null) playerInv.Remove(match);
            else return false;
        }
        return true;
    }
    private void OnEnable()
    {
        AutoMapMinigames();
        if (mg1_Prep == null || mg2_Slicing == null || mg3_Frying == null) return;
        mg1_Prep.OnStepDone += HandlePrepDone;
        mg2_Slicing.OnStepDone += HandleSlicingDone;
        mg3_Frying.OnStepDone += HandleFryingDone;
    }
    private void OnDisable()
    {
        if (mg1_Prep == null || mg2_Slicing == null || mg3_Frying == null) return;
        mg1_Prep.OnStepDone -= HandlePrepDone;
        mg2_Slicing.OnStepDone -= HandleSlicingDone;
        mg3_Frying.OnStepDone -= HandleFryingDone;
    }
    private void HandlePrepDone(float score)
    {
        _mg1Score = score;
        StartStep2();
    }
    private void HandleSlicingDone(float score)
    {
        _mg2Score = score;
        StartStep3();
    }
    private void HandleFryingDone(float score)
    {
        _mg3Score = score;
        FinalizeDish();
    }
    void StartStep1()
    {
        _avgFreshness = CalculateFreshness();
        if (mg1_Prep == null)
        {
            AutoMapMinigames();
        }
        if (mg1_Prep == null)
        {
            Debug.LogError("CookingManager: không tìm thấy PrepMinigame/pnl_PrepMinigame/pnl_Minigame_Prep trong scene.");
            return;
        }
        mg1_Prep.StartGame(_avgFreshness);
    }
    void StartStep2()
    {
        if (mg2_Slicing == null)
        {
            AutoMapMinigames();
        }
        if (mg2_Slicing == null)
        {
            Debug.LogError("CookingManager: không tìm thấy SlicingMinigame/pnl_SlicingMinigame/pnl_Minigame_Slicing trong scene.");
            return;
        }
        mg2_Slicing.StartGame(_avgFreshness);
    }
    void StartStep3()
    {
        if (mg3_Frying == null)
        {
            AutoMapMinigames();
        }
        if (mg3_Frying == null)
        {
            Debug.LogError("CookingManager: không tìm thấy FryingMinigame/pnl_FryingMinigame/pnl_Minigame_Frying trong scene.");
            return;
        }
        mg3_Frying.StartGame(_avgFreshness);
    }
    void FinalizeDish()
    {
        float avgSkill = (_mg1Score + _mg2Score + _mg3Score) / 3f;
        float H = 0.95f;
        float mAvg = avgSkill / 100f;
        float qFresh = _avgFreshness / 100f;
        float sFinal = (0.6f * mAvg + 0.4f * qFresh) / H;
        float finalScore = Mathf.Clamp01(sFinal) * 100f;
        float stars = finalScore / 20f;
        foreach (BaseItemSO req in _selectedRecipe.ingredientsRequired)
        {
            StoredItem itemInInv = PlayerInventory.Instance.carriedItems.Find(x => x.itemData.id == req.id);
            if (itemInInv != null)
            {
                itemInInv.quantity--; 
                if (itemInInv.quantity <= 0)
                {
                    PlayerInventory.Instance.carriedItems.Remove(itemInInv);
                }
            }
            StoredItem itemInBar = HotbarManager.Instance.items.Find(x => x.itemData.id == req.id);
            if (itemInBar != null)
            {
                itemInBar.quantity--;
                if (itemInBar.quantity <= 0)
                {
                    HotbarManager.Instance.items.Remove(itemInBar);
                }
            }
        }
        HotbarManager.Instance.RefreshUI();
        if (_selectedRecipe.dishResultSO != null)
        {
            HotbarManager.Instance.AddDish(_selectedRecipe.dishResultSO, stars);
        }
        else
        {
            Debug.LogError("LỖI: chưa kéo Dish Result SO vào file công thức " + _selectedRecipe.dishName);
        }
        CookingEvents.OnDishFinalized?.Invoke(_selectedRecipe, finalScore, stars);
        Debug.Log($"<color=yellow>HOÀN THÀNH: {_selectedRecipe.dishName} - Score: {finalScore:F1} - Rating: {stars:F1} SAO</color>");
        _selectedRecipe = null; 
    }
    float CalculateFreshness()
    {
        if (PlayerInventory.Instance.carriedItems.Count == 0) return 100;
        float total = 0;
        foreach (var i in PlayerInventory.Instance.carriedItems) total += i.currentFreshness;
        return total / PlayerInventory.Instance.carriedItems.Count;
    }
    public void CloseRecipeBook()
    {
        _selectedRecipe = null;
        CookingEvents.OnCloseRecipeBook?.Invoke();
    }
}
