using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StorageSlotUI : MonoBehaviour
{
    public Image imgIcon;
    public TextMeshProUGUI txtQty;
    public Slider slidFreshness;
    public Image fillImage; // Kéo phần Fill của Slider vào đây để đổi màu
    private StoredItem _targetItem;
    private Button _btn;

    public void Setup(StoredItem item)
    {
        _targetItem = item;
        imgIcon.sprite = item.itemData.icon;
        txtQty.text = "x" + item.quantity;
        _btn = GetComponent<Button>();
        _btn.onClick.RemoveAllListeners();
        _btn.onClick.AddListener(OnSlotClicked);
        UpdateUI();
    }

    private void Update()
    {
        if (_targetItem == null) return;
        UpdateUI();
    }

    private void UpdateUI()
    {
        float freshRatio = _targetItem.currentFreshness / 100f;
        slidFreshness.value = freshRatio;
        fillImage.color = Color.Lerp(Color.red, Color.green, freshRatio);
    }

    private void OnSlotClicked()
    {
        if (_targetItem.quantity > 0)
        {
            // 1. Trừ 1 món trong kho
            WarehouseManager.Instance.TakeItem(_targetItem);

            // 2. Bỏ vào túi của Alex
            PlayerInventory.Instance.AddItem(_targetItem);

            // 3. Cập nhật lại UI kho (để số lượng nhảy lùi)
            FindObjectOfType<WarehouseUI>().RefreshUI();
        }
    }

}