using UnityEngine;
using System.Collections.Generic;
using TMPro; // Để đổi tên tiêu đề kho nếu muốn

public class WarehouseUI : MonoBehaviour
{
    public GameObject slotPrefab;
    public Transform contentArea;
    public GameObject warehousePanel;
    public TextMeshProUGUI txtTitle; // (Tùy chọn) Để hiện chữ "KHO LẠNH" hoặc "KHO KHÔ"

    // Tạo một biến để nhớ loại kho đang mở
    private StorageType _currentFilter;

    // Hàm mở dành riêng cho Kho Lạnh
    public void OpenColdStorage()
    {
        _currentFilter = StorageType.Cold;
        if (txtTitle != null) txtTitle.text = "KHO LẠNH";
        ToggleWarehouse(true);
    }

    // Hàm mở dành riêng cho Kho Khô
    public void OpenDryStorage()
    {
        _currentFilter = StorageType.Dry;
        if (txtTitle != null) txtTitle.text = "KHO KHÔ";
        ToggleWarehouse(true);
    }

    public void ToggleWarehouse(bool isOpen)
    {
        warehousePanel.SetActive(isOpen);
        if (isOpen) RefreshUI();
    }

    public void ToggleWarehouse()
    {
        ToggleWarehouse(!warehousePanel.activeSelf);
    }

    public void RefreshUI()
    {
        foreach (Transform child in contentArea)
        {
            Destroy(child.gameObject);
        }

        // Kiểm tra loại kho đang chọn để đổ dữ liệu tương ứng
        if (_currentFilter == StorageType.Cold)
        {
            AddItemsToUI(WarehouseManager.Instance.coldStorage);
        }
        else
        {
            AddItemsToUI(WarehouseManager.Instance.dryStorage);
        }
    }

    private void AddItemsToUI(List<StoredItem> items)
    {
        foreach (var item in items)
        {
            GameObject newSlot = Instantiate(slotPrefab, contentArea);
            newSlot.GetComponent<StorageSlotUI>().Setup(item);
        }
    }
}