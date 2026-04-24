using UnityEngine;

public enum CustomerType { Chill, RichAndRush, FoodCritic }

[CreateAssetMenu(fileName = "so_Customer_", menuName = "Raumania/Customer Profile")]
public class CustomerProfileSO : ScriptableObject
{
    public CustomerType type;
    public string typeName;
    public float patienceDecayRate;
    public float tipMultiplier;   
    public Sprite avatar;
}