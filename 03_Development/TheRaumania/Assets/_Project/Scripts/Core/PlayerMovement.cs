using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    #region Biến cấu hình
    [Header("Cài đặt di chuyển")]
    [SerializeField] private float _moveSpeed = 5f;

    private Rigidbody2D _rb;
    private Vector2 _moveInput;
    private PlayerInputActions _inputActions; // Tên class sinh ra ở Bước 2
    #endregion

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _inputActions = new PlayerInputActions();
        _inputActions.Player.Interact.performed += OnInteractPressed;
    }

    // Bật/Tắt bộ nhận phím bấm
    private void OnEnable() => _inputActions.Player.Enable();
    private void OnDisable() => _inputActions.Player.Disable();

    private void OnInteractPressed(InputAction.CallbackContext context)
    {
        // Khi phím Interact được nhấn, gọi hàm tương tác của tất cả các đối tượng có thể tương tác trong phạm vi
        Interactable[] interactables = FindObjectsOfType<Interactable>();
        foreach (var obj in interactables)
        {
            obj.TriggerInteraction();
        }
    }

    private void Update()
    {
        // Đọc giá trị từ phím WASD (Trả về Vector2: X là ngang, Y là dọc)
        _moveInput = _inputActions.Player.Move.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        // Tính toán vận tốc dựa trên Input và Speed
        // Dùng FixedUpdate để di chuyển vật lý mượt mà, không bị giật
        _rb.velocity = _moveInput * _moveSpeed;
    }
}
