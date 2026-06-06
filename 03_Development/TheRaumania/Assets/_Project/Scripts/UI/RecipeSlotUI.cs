using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RecipeSlotUI : MonoBehaviour
{
    public Image imgIcon;
    public TextMeshProUGUI txtName;
    private RecipeSO _data;
    private Button _btn;

    public void Setup(RecipeSO data)
    {
        _data = data;
        imgIcon.sprite = data.dishIcon;
        txtName.text = data.dishName;
        _btn = GetComponent<Button>();
        _btn.onClick.RemoveAllListeners();
        _btn.onClick.AddListener(() => {
            CookingManager.Instance.SelectRecipe(_data);
        });
    }
}