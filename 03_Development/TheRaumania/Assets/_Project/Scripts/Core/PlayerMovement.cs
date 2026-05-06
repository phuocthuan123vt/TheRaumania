using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerMovement : MonoBehaviour
{
    #region Biến cấu hình
    [Header("Cài đặt di chuyển")]
    [SerializeField] private float _moveSpeed = 5f;
    private Rigidbody2D _rb;
    private Vector2 _moveInput;
    private PlayerInputActions _inputActions;
    private Animator _anim;
    #endregion
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _inputActions = new PlayerInputActions();
        _inputActions.Player.Interact.performed += OnInteractPressed;
        _anim = GetComponentInChildren<Animator>();
    }
    private void OnEnable() => _inputActions.Player.Enable();
    private void OnDisable() => _inputActions.Player.Disable();
    private void OnInteractPressed(InputAction.CallbackContext context)
    {
        Collider2D[] closeObjects = Physics2D.OverlapCircleAll(transform.position, 2f);
        foreach (var col in closeObjects)
        {
            if (col.TryGetComponent(out Interactable interactable))
            {
                interactable.TriggerInteraction();
                return;
            }
        }
    }
    private void Update()
    {
        _moveInput = _inputActions.Player.Move.ReadValue<Vector2>();
        if (_moveInput != Vector2.zero)
        {
            _anim.SetFloat("MoveX", _moveInput.x);
            _anim.SetFloat("MoveY", _moveInput.y);
            _anim.SetBool("IsMoving", true);
        }
        else
        {
            _anim.SetBool("IsMoving", false);
        }
    }
    private void FixedUpdate()
    {
        _rb.velocity = _moveInput * _moveSpeed;
    }
}
