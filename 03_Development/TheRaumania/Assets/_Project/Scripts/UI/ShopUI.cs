using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class ShopUI : MonoBehaviour
{
    public GameObject shopPanel;
    public GameObject shopSlotPrefab;
    public GameObject cartLinePrefab;
    public Transform shopContent, cartContent;
    public TextMeshProUGUI txtWallet, txtTotalBill, txtTotalQty;

    private void OnEnable()
    {
        ShopManager.OnCartChanged += RefreshCart;
        if (ShopManager.Instance != null)
        {
            RefreshCatalog();
            RefreshCart();
        }
    }

    private void OnDisable() { ShopManager.OnCartChanged -= RefreshCart; }

    private void Start()
    {
        RefreshCatalog();
        RefreshCart();
    }

    public void ToggleShop()
    {
        bool isOpening = !shopPanel.activeSelf;
        shopPanel.SetActive(isOpening);

        if (isOpening)
        {
            RefreshCatalog(); 
            RefreshCart(); 

            FindObjectOfType<PlayerMovement>().enabled = false;
        }
        else
        {
            FindObjectOfType<PlayerMovement>().enabled = true;
        }
    }

    void RefreshCatalog()
    {
        if (ShopManager.Instance == null) return;

        if (ShopManager.Instance.allItems == null) return;

        if (shopContent == null) return;

        foreach (Transform child in shopContent) Destroy(child.gameObject);
        foreach (var item in ShopManager.Instance.allItems)
        {
            Instantiate(shopSlotPrefab, shopContent).GetComponent<ShopSlotUI>().Setup(item);
        }
    }

    void RefreshCart()
    {
        foreach (Transform child in cartContent) Destroy(child.gameObject);

        int totalMoney = 0;
        int totalQty = 0; 

        foreach (var item in ShopManager.Instance.currentCart)
        {
            Instantiate(cartLinePrefab, cartContent).GetComponent<CartLineUI>().Setup(item);
            totalMoney += item.TotalPrice;
            totalQty += item.quantity; 
        }

        txtWallet.text = "Ví: " + PlayerData.rCredit + " RC";
        txtTotalBill.text = "Thành tiền: " + totalMoney + " RC";

        if (txtTotalQty != null)
            txtTotalQty.text = "Tổng số lượng: " + totalQty;
    }

    public void OnClickCheckout() { ShopManager.Instance.Checkout(); }
    public void OnClickCancel() { ShopManager.Instance.ClearCart(); }
}