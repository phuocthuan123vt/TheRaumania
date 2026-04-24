using UnityEngine;
using System;
using System.Collections.Generic;
public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;
    [Header("Data")]
    public List<BaseItemSO> allItems = new List<BaseItemSO>();
    public List<CartItem> currentCart = new List<CartItem>();
    private Dictionary<string, int> _globalSoldStats = new Dictionary<string, int>();
    public int totalStockLimit = 100;
    public static Action OnCartChanged;
    private void Awake() { Instance = this; }
    public int GetDynamicPrice(BaseItemSO item)
    {
        if (!_globalSoldStats.ContainsKey(item.id)) _globalSoldStats[item.id] = 0;
        float soldRatio = (float)_globalSoldStats[item.id] / totalStockLimit;
        float multiplier = 1f;
        if (soldRatio > 0.75f)
        {
            multiplier = 1f + (soldRatio - 0.75f) * 4f;
        }
        return Mathf.RoundToInt(item.basePrice * multiplier);
    }
    public void AddToCart(BaseItemSO item, int qty)
    {
        CartItem existing = currentCart.Find(x => x.itemData.id == item.id);
        int price = GetDynamicPrice(item);
        if (existing != null) existing.quantity += qty;
        else currentCart.Add(new CartItem(item, qty, price));
        OnCartChanged?.Invoke();
    }
    public void ClearCart()
    {
        currentCart.Clear();
        OnCartChanged?.Invoke();
    }
    public void Checkout()
    {
        int total = 0;
        foreach (var i in currentCart) total += i.TotalPrice;
        if (PlayerData.rCredit >= total)
        {
            PlayerData.rCredit -= total;
            foreach (var i in currentCart)
            {
                WarehouseManager.Instance.AddItemToWarehouse(i.itemData, i.quantity);
            }
            currentCart.Clear();
            OnCartChanged?.Invoke();
            Debug.Log("Mua hàng thành công!");
        }
    }
}
