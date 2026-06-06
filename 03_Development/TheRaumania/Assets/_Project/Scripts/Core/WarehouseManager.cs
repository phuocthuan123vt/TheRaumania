using UnityEngine;
using System.Collections.Generic;
public class WarehouseManager : MonoBehaviour
{
    public static WarehouseManager Instance;
    #region Variables
    [Header("Danh sách hàng hóa")]
    public List<StoredItem> dryStorage = new List<StoredItem>();
    public List<StoredItem> coldStorage = new List<StoredItem>();
    [Header("Cấu hình bảo quản")]
    [SerializeField] private float _coldStorageMultiplier = 0.2f;
    [SerializeField] private float _dryStorageMultiplier = 1.0f;
    #endregion
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void AddItemToWarehouse(BaseItemSO item, int qty)
    {
        StoredItem newItem = new StoredItem(item, qty);
        if (item.preferredStorage == StorageType.Cold)
            coldStorage.Add(newItem);
        else
            dryStorage.Add(newItem);
        Debug.Log($"Đã nhập {qty} {item.itemName} vào kho {item.preferredStorage}");
    }
    public void TakeItem(StoredItem item)
    {
        item.quantity--;
        if (item.quantity <= 0)
        {
            if (item.itemData.preferredStorage == StorageType.Cold)
                coldStorage.Remove(item);
            else
                dryStorage.Remove(item);
        }
        Debug.Log($"Đã lấy 1 {item.itemData.itemName} ra để nấu nướng!");
    }
    public void OnHourPassed()
    {
        foreach (var item in dryStorage) item.Decay(_dryStorageMultiplier);
        foreach (var item in coldStorage) item.Decay(_coldStorageMultiplier);

        Debug.Log("Một giờ game đã trôi qua, thực phẩm đang héo dần...");
    }
}
