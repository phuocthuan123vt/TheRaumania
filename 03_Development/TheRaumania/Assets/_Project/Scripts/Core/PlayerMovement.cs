using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    #region Biến cấu hình
    [Header("Cài đặt di chuyển")]
    [SerializeField] private float _moveSpeed = 5f;
    private Rigidbody2D _rb;
    private Vector2 _moveInput;
    private Vector2 _lastMoveDir = Vector2.down;
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
        // Nếu Cheat Console đang mở thì không cho tương tác
        if (CheatConsole.Instance != null && CheatConsole.Instance.IsConsoleOpen) return;

        Collider2D[] closeObjects = Physics2D.OverlapCircleAll(transform.position, 2f);
        var interactables = new List<Interactable>();
        foreach (var col in closeObjects)
        {
            if (col.TryGetComponent(out Interactable interactable)) interactables.Add(interactable);
        }
        if (interactables.Count == 0) return;

        // Find minimum distance
        float minDist = float.MaxValue;
        foreach (var it in interactables)
        {
            float d = Vector2.Distance(transform.position, it.transform.position);
            if (d < minDist) minDist = d;
        }

        // Candidates within threshold of nearest — prefer the one player is facing
        const float threshold = 0.5f;
        Interactable best = null;
        float bestDot = -1f;
        Vector2 facing = _lastMoveDir.sqrMagnitude > 0 ? _lastMoveDir.normalized : Vector2.down;
        foreach (var it in interactables)
        {
            float d = Vector2.Distance(transform.position, it.transform.position);
            if (d <= minDist + threshold)
            {
                Vector2 dir = ((Vector2)it.transform.position - (Vector2)transform.position).normalized;
                float dot = Vector2.Dot(facing, dir);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    best = it;
                }
            }
        }
        if (best == null) best = interactables[0];
        best.TriggerInteraction(transform.position);
    }

    private void Update()
    {
        // Kiểm tra xem người chơi có đang nhập lệnh không, nếu có thì chặn di chuyển
        if (CheatConsole.Instance != null && CheatConsole.Instance.IsConsoleOpen)
        {
            _moveInput = Vector2.zero;
            _anim.SetBool("IsMoving", false);
            return;
        }

        _moveInput = _inputActions.Player.Move.ReadValue<Vector2>();
        if (_moveInput != Vector2.zero)
        {
            _anim.SetFloat("MoveX", _moveInput.x);
            _anim.SetFloat("MoveY", _moveInput.y);
            _anim.SetBool("IsMoving", true);
            _lastMoveDir = _moveInput.normalized;
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
