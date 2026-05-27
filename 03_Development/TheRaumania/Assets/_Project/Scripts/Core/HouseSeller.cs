using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class HouseSeller : MonoBehaviour
{
    public string sellerName = "HouseSeller";

    // Called by Interactable when player presses E
    public void OnInteractCalled()
    {
        Debug.Log($"Player interacted with {sellerName}.");
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.ShowUpgradeDialog();
        }
        else
        {
            Debug.LogWarning("UpgradeManager not found in scene.");
        }
    }
}
