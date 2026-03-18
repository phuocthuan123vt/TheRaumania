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
    }

    // Bật/Tắt bộ nhận phím bấm
    private void OnEnable() => _inputActions.Player.Enable();
    private void OnDisable() => _inputActions.Player.Disable();

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
