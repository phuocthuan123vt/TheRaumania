using UnityEngine;
using System.Collections.Generic;

public class RecipeBookUI : MonoBehaviour
{
    public GameObject recipeSlotPrefab; // Kéo pf_RecipeSlot vào đây
    public Transform contentArea;       // Kéo cái Content của Scroll View vào đây
    public List<RecipeSO> allRecipes;   // Danh sách các món Alex biết nấu

    private void OnEnable()
    {
        // Xóa các ô cũ
        foreach (Transform child in contentArea) Destroy(child.gameObject);

        // Tạo ô món ăn mới từ danh sách
        foreach (var recipe in allRecipes)
        {
            GameObject newSlot = Instantiate(recipeSlotPrefab, contentArea);
            newSlot.GetComponent<RecipeSlotUI>().Setup(recipe);
        }
    }
}