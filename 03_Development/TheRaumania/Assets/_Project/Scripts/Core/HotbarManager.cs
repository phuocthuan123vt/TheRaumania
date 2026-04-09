using UnityEngine;
using System.Collections.Generic;

public class HotbarManager : MonoBehaviour
{
    public static HotbarManager Instance;
    public List<StoredItem> items = new List<StoredItem>();
    public HotbarSlotUI[] uiSlots; // Kéo 10 ô ở Hierarchy vào đây theo đúng thứ tự

    private void Awake() { Instance = this; }

    private void Start() { RefreshUI(); }

    public void AddDish(BaseItemSO dishData, float stars)
    {
        if (items == null) items = new List<StoredItem>();

        StoredItem existingInBar = items.Find(x => x.itemData.id == dishData.id);

        if (existingInBar != null)
        {
            if (existingInBar.quantity < 999)
            {
                existingInBar.quantity++;
                existingInBar.currentFreshness = (existingInBar.currentFreshness + (stars * 20f)) / 2f;
            }
        }
        else
        {
            if (items.Count < 10)
            {
                StoredItem newEntry = new StoredItem(dishData, 1);
                newEntry.currentFreshness = stars * 20f;
                items.Add(newEntry);
            }
        }

        RefreshUI();
    }
    [ContextMenu("Refresh Hotbar UI")]
    public void RefreshUI()
    {
        for (int i = 0; i < uiSlots.Length; i++)
        {
            if (i < items.Count)
                uiSlots[i].Setup(items[i]);
            else
                uiSlots[i].Clear();
        }
    }

    public void RemoveItemFromHotbar(BaseItemSO data)
    {
        StoredItem itemInBar = items.Find(x => x.itemData.id == data.id);

        if (itemInBar != null)
        {
            items.Remove(itemInBar);
            RefreshUI(); 
        }
    }

    private void OnValidate()
    {
        if (Instance != null) RefreshUI();
    }
}