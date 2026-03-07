using RobsonRocha.UnityCommon;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Moveable : MonoBehaviour
{
    [SerializeField] private MovementMode Mode = MovementMode.WalkAndRun;
    [SerializeField] private FacingMode FacingMode = FacingMode.AllDirections;
    [SerializeField] private float MoveSpeed = 3f;
    [SerializeField] private float RunSpeedMultiplier = 2f;

    private Rigidbody2D _rb;
    private Vector2 _movementDirection;
    private bool _isRunning;
    private float _lastHorizontal = 0;
    private float _lastVertical = -1;
    private Vector2? _externalVelocity;

    public bool IsMoving => _externalVelocity.HasValue || _movementDirection.sqrMagnitude > 0.01f;
    public bool IsRunning => IsMoving && _isRunning;
    public bool IsIdling => !IsMoving;

    public bool IsFacingUp => _lastVertical > 0;
    public bool IsFacingDown => _lastVertical < 0;
    public bool IsFacingRight => _lastHorizontal > 0;
    public bool IsFacingLeft => _lastHorizontal < 0;

    public Vector2 FacingDirection => new Vector2(_lastHorizontal, _lastVertical).normalized;
    public FacingMode CurrentFacingMode => FacingMode;

    private void Awake()
    {
        this.TryInitComponent(ref _rb);
    }

    private void FixedUpdate()
    {
        if (_rb != null)
        {
            if (_externalVelocity.HasValue)
            {
                _rb.linearVelocity = _externalVelocity.Value;
            }
            else
            {
                float currentSpeed = _isRunning ? MoveSpeed * RunSpeedMultiplier : MoveSpeed;
                _rb.linearVelocity = _movementDirection.normalized * currentSpeed;
            }
        }
    }

    public void Move(Vector2 direction, bool run = false)
    {
        _movementDirection = direction;
        _isRunning = DetermineRunState(run);

        if (direction.sqrMagnitude > 0.01f)
        {
            UpdateFacingDirection(direction);
        }
    }

    private bool DetermineRunState(bool run)
    {
        return Mode switch
        {
            MovementMode.RunOnly => true,
            MovementMode.WalkOnly => false,
            MovementMode.WalkAndRun => run,
            _ => false
        };
    }

    public void FaceDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude > 0.01f)
        {
            UpdateFacingDirection(direction);
        }
    }

    private void UpdateFacingDirection(Vector2 direction)
    {
        Vector2 normalized = direction.normalized;
        float absX = Mathf.Abs(normalized.x);
        float absY = Mathf.Abs(normalized.y);

        if (FacingMode == FacingMode.HorizontalOnly)
        {
            _lastHorizontal = absX > 0.01f ? Mathf.Sign(normalized.x) : _lastHorizontal;
            _lastVertical = 0;
        }
        else if (FacingMode == FacingMode.VerticalOnly)
        {
            _lastHorizontal = 0;
            _lastVertical = absY > 0.01f ? Mathf.Sign(normalized.y) : _lastVertical;
        }
        else
        {
            if (absX > absY)
            {
                _lastHorizontal = Mathf.Sign(normalized.x);
                _lastVertical = 0;
            }
            else
            {
                _lastHorizontal = 0;
                _lastVertical = Mathf.Sign(normalized.y);
            }
        }
    }

    public void Stop()
    {
        _movementDirection = Vector2.zero;
        _isRunning = false;
    }

    /// <summary>
    /// Sets an external velocity that overrides normal movement calculations.
    /// Used for effects like knockback that need precise velocity control.
    /// </summary>
    /// <param name="velocity">The velocity to apply</param>
    /// <param name="facingDirection">Optional direction to face while moving</param>
    public void SetExternalVelocity(Vector2 velocity, Vector2? facingDirection = null)
    {
        _externalVelocity = velocity;
        if (facingDirection.HasValue)
        {
            UpdateFacingDirection(facingDirection.Value);
        }
    }

    /// <summary>
    /// Clears the external velocity override, returning to normal movement.
    /// </summary>
    public void ClearExternalVelocity()
    {
        _externalVelocity = null;
    }
}
