using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

public class HotbarManager : MonoBehaviour
{
    public static HotbarManager Instance;
    public List<StoredItem> items = new List<StoredItem>();
    public HotbarSlotUI[] uiSlots; // Kéo 10 ô ở Hierarchy vào đây theo đúng thứ tự
    
    public int SelectedSlotIndex { get; private set; } = 0; // Thêm biến lưu vị trí đang chọn

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Keep the hotbar manager across scenes to preserve items
            DontDestroyOnLoad(this.transform.root.gameObject);
        }
        else if (Instance != this)
        {
            // Avoid duplicate instances overriding the singleton
            Destroy(this);
            return;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Khi scene thay đổi có thể xuất hiện panel hotbar mới trong scene,
        // thử remap lại UI slots và refresh UI để tránh NRE.
        AutoMapUISlotsIfNeeded();
        RefreshUI();
    }

    private void AutoMapUISlotsIfNeeded()
    {
        bool needsMapping = (uiSlots == null || uiSlots.Length == 0 || uiSlots.Any(s => s == null));
        if (!needsMapping) return;

        // Try to find the panel named "pnl_Hotbar" in scene (common in our prefabs)
        GameObject panel = GameObject.Find("pnl_Hotbar");
        HotbarSlotUI[] found = null;
        if (panel != null)
        {
            found = panel.GetComponentsInChildren<HotbarSlotUI>(true);
            // Order by sibling index under the panel to preserve slot order
            found = found.OrderBy(h => h.transform.GetSiblingIndex()).ToArray();
        }

        // Fallback: find any HotbarSlotUI in scene
        if (found == null || found.Length == 0)
        {
            found = GameObject.FindObjectsOfType<HotbarSlotUI>(true);
            if (found != null && found.Length > 0)
            {
                // Try to order by hierarchy depth then sibling index to get consistent order
                found = found.OrderBy(h => h.transform.GetSiblingIndex()).ToArray();
            }
        }

        if (found != null && found.Length > 0)
        {
            // Ensure we only map up to 10 slots (hotbar has 10 slots)
            int take = Mathf.Min(10, found.Length);
            uiSlots = new HotbarSlotUI[take];
            for (int i = 0; i < take; i++) uiSlots[i] = found[i];

            string names = string.Join(", ", uiSlots.Select(s => s != null ? s.gameObject.name : "null"));
            Debug.Log($"HotbarManager: auto-mapped {uiSlots.Length} ui slots from scene: {names}");
        }
    }

    private void Start() { AutoMapUISlotsIfNeeded(); RefreshUI(); }

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
        if (uiSlots == null || uiSlots.Length == 0)
        {
            Debug.LogWarning("HotbarManager: uiSlots not mapped yet; cannot select slot.");
            return;
        }

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
        int slotCapacity = (uiSlots != null && uiSlots.Length > 0) ? uiSlots.Length : 10;
        if (items.Count < slotCapacity)
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
        if (uiSlots == null || uiSlots.Length == 0)
        {
            Debug.Log("HotbarManager: RefreshUI skipped because uiSlots not mapped.");
            return;
        }

        for (int i = 0; i < uiSlots.Length; i++)
        {
            if (i < items.Count && items[i] != null)
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