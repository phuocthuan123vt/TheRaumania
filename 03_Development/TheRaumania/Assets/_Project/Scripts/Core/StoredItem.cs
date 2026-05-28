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

        // decayRate được hiểu là mức giảm theo mỗi giờ game
        currentFreshness -= itemData.decayRate * multiplier;
        currentFreshness = Mathf.Clamp(currentFreshness, 0, 100);
    }
}