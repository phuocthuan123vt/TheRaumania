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

    public void Setup(StoredItem item)
    {
        _targetItem = item;
        imgIcon.sprite = item.itemData.icon;
        txtQty.text = "x" + item.quantity;
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

}