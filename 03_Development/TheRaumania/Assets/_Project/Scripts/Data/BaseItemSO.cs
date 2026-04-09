using UnityEngine;

public enum StorageType { Dry, Cold } // Đồ khô hoặc Đồ lạnh

[CreateAssetMenu(fileName = "so_NewItem", menuName = "Raumania/Item")]
public class BaseItemSO : ScriptableObject
{
    public string id;
    public string itemName;
    public Sprite icon;
    public int basePrice;

    [Header("Freshness Logic")]
    public bool isPerishable;      // Có bị hỏng theo thời gian không?
    public float decayRate;        // Tốc độ giảm độ tươi (điểm/giờ)
    public StorageType preferredStorage;
}