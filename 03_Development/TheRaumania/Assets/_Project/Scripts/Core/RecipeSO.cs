using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "so_NewRecipe", menuName = "Raumania/Recipe")]
public class RecipeSO : ScriptableObject
{
    public BaseItemSO dishResultSO;
    public string dishName;
    public Sprite dishIcon;
    public List<BaseItemSO> ingredientsRequired; 
    public int baseValue; 
}