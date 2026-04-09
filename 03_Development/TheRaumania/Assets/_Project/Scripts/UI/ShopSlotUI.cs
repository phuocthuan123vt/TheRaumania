using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopSlotUI : MonoBehaviour
{
    public Image imgIcon;
    public TextMeshProUGUI txtName, txtPrice, txtBuyQty;
    private BaseItemSO _data;
    private int _qty = 1;

    public void Setup(BaseItemSO item)
    {
        _data = item;
        imgIcon.sprite = item.icon;
        txtName.text = item.itemName;
        txtPrice.text = ShopManager.Instance.GetDynamicPrice(item) + " RC";
        txtBuyQty.text = _qty.ToString();
    }

    public void ChangeQty(int amount)
    {
        _qty = Mathf.Max(1, _qty + amount);
        txtBuyQty.text = _qty.ToString();
    }

    public void AddToCart()
    {
        ShopManager.Instance.AddToCart(_data, _qty);
        _qty = 1;
        txtBuyQty.text = "1";
    }
}