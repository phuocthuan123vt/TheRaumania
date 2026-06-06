using UnityEngine;
using UnityEngine.Events;
public class Interactable : MonoBehaviour
{
    #region Variables
    [Header("Cài đặt tương tác")]
    public string interactMessage = "Nhấn E để tương tác";
    public float interactRange = 2f;
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
    // Backwards-compatible no-arg trigger: will try to find Player and use its position.
    public void TriggerInteraction()
    {
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            TriggerInteraction((Vector2)player.transform.position);
        }
        else if (_isPlayerInRange)
        {
            Debug.Log($"Đang tương tác với: {gameObject.name}");
            onInteract.Invoke();
        }
    }

    // New: distance-based interaction to avoid relying solely on trigger enter/exit.
    public void TriggerInteraction(Vector2 playerPosition)
    {
        if (Vector2.Distance(playerPosition, transform.position) <= interactRange)
        {
            Debug.Log($"Đang tương tác với: {gameObject.name} (range ok)");
            onInteract.Invoke();
        }
        else
        {
            Debug.Log($"Quá xa để tương tác với: {gameObject.name}");
        }
    }
}
