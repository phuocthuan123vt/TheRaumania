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

        // Lấy component Button ngay trên ô này
        _btn = GetComponent<Button>();

        // Xóa các lệnh cũ để tránh bấm 1 lần chạy 2 món
        _btn.onClick.RemoveAllListeners();

        // Cắm dây lệnh: Khi bấm vào ô này, gọi hàm Select của CookingManager
        _btn.onClick.AddListener(() => {
            CookingManager.Instance.SelectRecipe(_data);
        });
    }
}