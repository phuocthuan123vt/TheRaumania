using UnityEngine;
using UnityEngine.Events; // Cần cái này để dùng UnityEvent

public class Interactable : MonoBehaviour
{
    #region Variables
    [Header("Cài đặt tương tác")]
    public string interactMessage = "Nhấn E để tương tác";
    public UnityEvent onInteract; // Đây là "ổ cắm" - ông sẽ kéo lệnh vào đây từ Inspector

    private bool _isPlayerInRange = false;
    #endregion

    // Khi người chơi đi vào vùng cảm biến (Collider2D isTrigger)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerInRange = true;
            Debug.Log(interactMessage);
            // Sau này ông sẽ gọi UI hiện lên ở đây
        }
    }

    // Khi người chơi đi ra khỏi vùng cảm biến
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerInRange = false;
        }
    }

    // Hàm này sẽ được gọi từ PlayerMovement khi người chơi nhấn phím E
    public void TriggerInteraction()
    {
        if (_isPlayerInRange)
        {
            Debug.Log($"Đang tương tác với: {gameObject.name}");
            onInteract.Invoke(); // Kích hoạt tất cả các lệnh đã cắm vào
        }
    }
}