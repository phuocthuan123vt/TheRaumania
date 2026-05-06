using UnityEngine;
using System.Collections.Generic;

public class HotbarManager : MonoBehaviour
{
    public static HotbarManager Instance;
    public List<StoredItem> items = new List<StoredItem>();
    public HotbarSlotUI[] uiSlots; // Kéo 10 ô ở Hierarchy vào đây theo đúng thứ tự
    
    public int SelectedSlotIndex { get; private set; } = 0; // Thêm biến lưu vị trí đang chọn

    private void Awake() { Instance = this; }

    private void Start() { RefreshUI(); }

    private void Update()
    {
        // Chuyển slot bằng phím số 1-9
        for (int i = 0; i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SelectSlot(i);
            }
        }
        // Phím 0 cho slot thứ 10 (index 9)
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            SelectSlot(9);
        }
    }

    public void SelectSlot(int index)
    {
        if (index >= 0 && index < uiSlots.Length)
        {
            SelectedSlotIndex = index;
            // TODO: Ở đây bạn có thể gọi hàm cập nhật UI hiển thị viền vàng quanh slot đang chọn
            Debug.Log($"Đang chọn Slot: {index + 1}");
            RefreshUI();
        }
    }

    public void AddDish(BaseItemSO dishData, float stars)
    {
        if (items == null) items = new List<StoredItem>();

        // Chỉ kiểm tra item stack nếu item này TỪ CHỐI TÁCH RIÊNG (Không phải thành phẩm món ăn)
        // Đề bài yêu cầu: món ăn (dish) không stack, phải ném vào slot mới
        if (dishData.itemType != "Dish") // Giả sử "Dish" là tag phân loại món ăn nấu ra. Nếu không phải Dish thì cho stack
        {
            StoredItem existingInBar = items.Find(x => x.itemData.id == dishData.id);
            if (existingInBar != null)
            {
                if (existingInBar.quantity < 999)
                {
                    existingInBar.quantity++;
                    existingInBar.currentFreshness = (existingInBar.currentFreshness + (stars * 20f)) / 2f;
                    RefreshUI();
                    return;
                }
            }
        }
        
        // Món ăn hoặc không có sẵn thì tạo ô mới
        if (items.Count < uiSlots.Length)
        {
            StoredItem newEntry = new StoredItem(dishData, 1);
            newEntry.currentFreshness = stars * 20f;
            items.Add(newEntry);
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