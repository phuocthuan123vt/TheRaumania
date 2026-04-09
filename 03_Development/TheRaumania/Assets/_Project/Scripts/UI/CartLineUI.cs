using UnityEngine;
using TMPro;

public class CartLineUI : MonoBehaviour
{
    public TextMeshProUGUI txtInfo; // Ví dụ: "Cà rốt x 12"
    public TextMeshProUGUI txtTotal; // Ví dụ: "600 RC"

    public void Setup(CartItem item)
    {
        txtInfo.text = $"{item.itemData.itemName} x {item.quantity}";
        txtTotal.text = $"{item.TotalPrice} RC";
    }
}