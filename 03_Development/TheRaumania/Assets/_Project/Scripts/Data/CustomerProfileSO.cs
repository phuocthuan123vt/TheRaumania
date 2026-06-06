using UnityEngine;

public enum CustomerType { Chill, RichAndRush, FoodCritic }

[CreateAssetMenu(fileName = "so_Customer_", menuName = "Raumania/Customer Profile")]
public class CustomerProfileSO : ScriptableObject
{
    public CustomerType type;
    public string typeName;
    public float patienceDecayRate;
    public float tipMultiplier;   
    [Range(0.5f, 2f)] public float hygieneSensitivity = 1f;
    [Range(0f, 2f)] public float windowSeatMoodBonus = 0.15f;
    [Range(0f, 0.2f)] public float randomReactionChance = 0.04f;
    public Sprite avatar;
}