using UnityEngine;
using System.Collections.Generic;

public class CookingManager : MonoBehaviour
{
    public static CookingManager Instance;

    [Header("Minigame Panels")]
    public PrepMinigame mg1_Prep;
    public SlicingMinigame mg2_Slicing;
    public FryingMinigame mg3_Frying;

    [Header("UI Panels")]
    public GameObject pnl_RecipeBook;

    private RecipeSO _selectedRecipe;
    private float _mg1Score, _mg2Score, _mg3Score;
    private float _avgFreshness;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void OpenRecipeBook()
    {
        pnl_RecipeBook.SetActive(true);
        _selectedRecipe = null; // Reset mỗi lần mở
    }

    public void SelectRecipe(RecipeSO recipe)
    {
        _selectedRecipe = recipe;
        Debug.Log("<color=cyan>Đã chọn món: </color>" + recipe.dishName);
    }

    public void OnClickStartCooking()
    {
        if (_selectedRecipe == null)
        {
            Debug.Log("<color=red>Chưa chọn món ông ơi!</color>");
            return;
        }

        if (CanCook(_selectedRecipe))
        {
            pnl_RecipeBook.SetActive(false);
            StartStep1();
        }
        else
        {
            Debug.Log("<color=orange>Alex ơi, thiếu nguyên liệu rồi!</color>");
        }
    }

    private bool CanCook(RecipeSO recipe)
    {
        // Tạo bản sao túi đồ để kiểm tra
        List<StoredItem> playerInv = new List<StoredItem>(PlayerInventory.Instance.carriedItems);
        foreach (var req in recipe.ingredientsRequired)
        {
            StoredItem match = playerInv.Find(x => x.itemData.id == req.id);
            if (match != null) playerInv.Remove(match);
            else return false;
        }
        return true;
    }

    // --- LUỒNG CHUYỂN BƯỚC ---
    void StartStep1()
    {
        _avgFreshness = CalculateFreshness();
        mg1_Prep.OnStepDone = (s) => { _mg1Score = s; StartStep2(); };
        mg1_Prep.StartGame(_avgFreshness);
    }

    void StartStep2()
    {
        mg2_Slicing.OnStepDone = (s) => { _mg2Score = s; StartStep3(); };
        mg2_Slicing.StartGame(_avgFreshness);
    }

    void StartStep3()
    {
        mg3_Frying.OnStepDone = (s) => { _mg3Score = s; FinalizeDish(); };
        mg3_Frying.StartGame(_avgFreshness);
    }

    void FinalizeDish()
    {
        float avgSkill = (_mg1Score + _mg2Score + _mg3Score) / 3f;

        float finalScore = (avgSkill * 0.7f) + (_avgFreshness * 0.3f);

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
        Debug.Log($"<color=yellow>HOÀN THÀNH: {_selectedRecipe.dishName} - Đánh giá: {stars:F1} SAO</color>");
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
        pnl_RecipeBook.SetActive(false);
        _selectedRecipe = null;
    }
}
