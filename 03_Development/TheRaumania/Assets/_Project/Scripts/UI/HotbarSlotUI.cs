using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HotbarSlotUI : MonoBehaviour
{
    public Image imgIcon;
    public TextMeshProUGUI txtQty;
    public TextMeshProUGUI txtStars;
    public GameObject content;

    public void Setup(StoredItem item)
    {
        if (content != null) content.SetActive(true);
        imgIcon.sprite = item.itemData.icon;
        txtQty.text = "x" + item.quantity;

        float stars = item.currentFreshness / 20f;
        txtStars.text = stars.ToString("F1") + "*"; 

    }

    public void Clear()
    {
        if (content != null) content.SetActive(false);
    }
}