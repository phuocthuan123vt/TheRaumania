using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    public GameObject shopPanel;
    public GameObject shopSlotPrefab;
    public GameObject cartLinePrefab;
    public Transform shopContent, cartContent;
    public TextMeshProUGUI txtWallet, txtTotalBill, txtTotalQty;

    private void Awake()
    {
        AutoMapUI();
        AutoWireButtons();
    }

    private void AutoMapUI()
    {
        Transform root = transform.root;
        if (shopPanel == null) shopPanel = RuntimeReferenceFinder.FindDeepGameObject(root, "pnl_Shop");
        if (shopContent == null)
        {
            GameObject found = RuntimeReferenceFinder.FindDeepGameObject(root, "shopContent");
            if (found != null) shopContent = found.transform;
        }
        if (cartContent == null)
        {
            GameObject found = RuntimeReferenceFinder.FindDeepGameObject(root, "cartContent");
            if (found != null) cartContent = found.transform;
        }
        if (txtWallet == null) txtWallet = RuntimeReferenceFinder.FindDeepComponent<TextMeshProUGUI>(root, "txt_Wallet");
        if (txtTotalBill == null) txtTotalBill = RuntimeReferenceFinder.FindDeepComponent<TextMeshProUGUI>(root, "txt_TotalBill");
        if (txtTotalQty == null) txtTotalQty = RuntimeReferenceFinder.FindDeepComponent<TextMeshProUGUI>(root, "txt_TotalQty");
    }

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
        AutoMapUI();
        AutoWireButtons();
        RefreshCatalog();
        RefreshCart();
    }

    private void AutoWireButtons()
    {
        if (shopPanel == null) return;

        WireButton("btn_Checkout", OnClickCheckout);
        WireButton("btn_Cancel", OnClickCancel);
    }

    private void WireButton(string buttonName, UnityEngine.Events.UnityAction action)
    {
        if (string.IsNullOrEmpty(buttonName) || action == null) return;

        Button button = RuntimeReferenceFinder.FindDeepComponent<Button>(shopPanel.transform, buttonName);
        if (button == null) return;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
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

        txtWallet.text = "Ví: " + PlayerData.RCredit + " RC";
        txtTotalBill.text = "Thành tiền: " + totalMoney + " RC";

        if (txtTotalQty != null)
            txtTotalQty.text = "Tổng số lượng: " + totalQty;
    }

    public void OnClickCheckout() { ShopManager.Instance.Checkout(); }
    public void OnClickCancel() { ShopManager.Instance.ClearCart(); }
}