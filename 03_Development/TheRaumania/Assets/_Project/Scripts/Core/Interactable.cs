using UnityEngine;
using UnityEngine.Events;
public class Interactable : MonoBehaviour
{
    #region Variables
    [Header("Cài đặt tương tác")]
    public string interactMessage = "Nhấn E để tương tác";
    public UnityEvent onInteract;
    private bool _isPlayerInRange = false;
    #endregion
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerInRange = true;
            Debug.Log(interactMessage);
        }
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerInRange = false;
        }
    }
    public void TriggerInteraction()
    {
        if (_isPlayerInRange)
        {
            Debug.Log($"Đang tương tác với: {gameObject.name}");
            onInteract.Invoke();
        }
    }
}
