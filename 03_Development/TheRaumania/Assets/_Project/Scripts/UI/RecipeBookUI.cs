using UnityEngine;
using System.Collections.Generic;

public class RecipeBookUI : MonoBehaviour
{
    public GameObject recipeSlotPrefab; 
    public Transform contentArea;      
    public List<RecipeSO> allRecipes;   

    private void OnEnable()
    {
        foreach (Transform child in contentArea) Destroy(child.gameObject);
        foreach (var recipe in allRecipes)
        {
            GameObject newSlot = Instantiate(recipeSlotPrefab, contentArea);
            newSlot.GetComponent<RecipeSlotUI>().Setup(recipe);
        }
    }
}