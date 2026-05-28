using UnityEngine;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance;

    public List<StoredItem> carriedItems = new List<StoredItem>();

    private void Awake() { Instance = this; }

    public void AddItem(StoredItem item)
    {
        StoredItem existingItem = carriedItems.Find(x => x.itemData.id == item.itemData.id);

        if (existingItem != null)
        {
            if (existingItem.quantity < 999)
            {
                existingItem.quantity++;
                existingItem.currentFreshness = Mathf.Max(existingItem.currentFreshness, item.currentFreshness);
            }
        }
        else
        {
            StoredItem newItem = new StoredItem(item.itemData, 1);
            newItem.currentFreshness = item.currentFreshness;
            carriedItems.Add(newItem);
        }
        HotbarManager.Instance.AddDish(item.itemData, item.currentFreshness / 20f);
    }

    public void RemoveItem(BaseItemSO data)
    {
        StoredItem itemToRemove = carriedItems.Find(x => x.itemData.id == data.id);

        if (itemToRemove != null)
        {
            carriedItems.Remove(itemToRemove);
            Debug.Log($"Đã dùng: {data.itemName}, trong túi vẫn còn {carriedItems.Count} món khác.");
        }
    }

    public void ClearInventory()
    {
        carriedItems.Clear();
    }
}