using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HotbarSlotUI : MonoBehaviour
{
    public Image imgIcon;
    public TextMeshProUGUI txtQty;
    public TextMeshProUGUI txtStars;
    public GameObject content;
    public GameObject selectedHighlight; // assign a child GameObject to show selection (outline, glow, etc.)

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
        if (selectedHighlight != null) selectedHighlight.SetActive(false);
    }

    public void SetSelected(bool isSelected)
    {
        if (selectedHighlight != null)
        {
            selectedHighlight.SetActive(isSelected);
        }
        else
        {
            // fallback: change icon color to indicate selection
            if (imgIcon != null)
            {
                imgIcon.color = isSelected ? Color.yellow : Color.white;
            }
        }
    }
}