using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StorageSlotUI : MonoBehaviour
{
    public Image imgIcon;
    public TextMeshProUGUI txtQty;
    public Slider slidFreshness;
    public Image fillImage; 
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
            WarehouseManager.Instance.TakeItem(_targetItem);
            PlayerInventory.Instance.AddItem(_targetItem);
            FindObjectOfType<WarehouseUI>().RefreshUI();
        }
    }

}