using UnityEngine;
using System.Collections.Generic;

public class WarehouseManager : MonoBehaviour
{
    // Singleton để các script khác (Shop, Cooking) dễ truy cập
    public static WarehouseManager Instance;

    #region Variables
    [Header("Danh sách hàng hóa")]
    public List<StoredItem> dryStorage = new List<StoredItem>();
    public List<StoredItem> coldStorage = new List<StoredItem>();

    [Header("Cấu hình bảo quản")]
    [SerializeField] private float _coldStorageMultiplier = 0.2f; // Kho lạnh làm chậm hỏng 5 lần
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

    private void Update()
    {
        // Cập nhật độ tươi cho toàn bộ hàng trong kho mỗi khung hình
        UpdateAllFreshness();
    }

    private void UpdateAllFreshness()
    {
        foreach (var item in dryStorage) item.Decay(_dryStorageMultiplier);
        foreach (var item in coldStorage) item.Decay(_coldStorageMultiplier);
    }

    // Hàm để Shop gọi khi mua hàng thành công
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

        // Nếu hết hàng thì xóa khỏi danh sách tương ứng
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
        // Mỗi giờ trôi qua, giảm độ tươi mạnh hơn (ví dụ giảm 5 điểm)
        foreach (var item in dryStorage) item.currentFreshness -= 1;
        foreach (var item in coldStorage) item.currentFreshness -= 0.5f; // Kho lạnh giảm ít hơn

        Debug.Log("Một giờ game đã trôi qua, thực phẩm đang héo dần...");
    }
}