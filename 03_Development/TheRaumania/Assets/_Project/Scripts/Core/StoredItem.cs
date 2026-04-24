using UnityEngine;
using System;

[Serializable]
public class StoredItem
{
    public BaseItemSO itemData;
    public float currentFreshness = 100f; // Độ tươi từ 0 - 100
    public int quantity;

    public StoredItem(BaseItemSO data, int qty)
    {
        itemData = data;
        quantity = qty;
        currentFreshness = 100f; // Mới mua nên tươi 100%
    }

    // Hàm giảm độ tươi theo thời gian
    public void Decay(float multiplier)
    {
        if (!itemData.isPerishable) return;

        // Công thức: giảm theo decayRate của Item
        currentFreshness -= itemData.decayRate * multiplier * Time.deltaTime;
        currentFreshness = Mathf.Clamp(currentFreshness, 0, 100);
    }
}