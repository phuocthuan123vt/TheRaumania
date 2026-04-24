using UnityEngine;

public enum StorageType { Dry, Cold }

[CreateAssetMenu(fileName = "so_NewItem", menuName = "Raumania/Item")]
public class BaseItemSO : ScriptableObject
{
    public string id;
    public string itemName;
    public Sprite icon;
    public int basePrice;

    [Header("Freshness Logic")]
    public bool isPerishable;
    public float decayRate; 
    public StorageType preferredStorage;

    [Header("Tags")]
    public string itemType;
}