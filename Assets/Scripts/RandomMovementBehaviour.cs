using RobsonRocha.UnityCommon;
using UnityEngine;

[RequireComponent(typeof(Moveable))]
[DefaultExecutionOrder(10)]
public class RandomMovementBehaviour : MonoBehaviour, IBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float MinMoveDuration = 0.5f;
    [SerializeField] private float MaxMoveDuration = 2f;
    [SerializeField] private float MinIdleDuration = 0.5f;
    [SerializeField] private float MaxIdleDuration = 3f;
    [SerializeField][Range(0f, 1f)] private float OnlyFaceChance = 0.2f;

    [Header("Detection Settings")]
    [SerializeField][Range(0f, 1f)] private float DetectionConeThreshold = 0.3f;
    [SerializeField][Range(0f, 1f)] private float CollisionDirectionThreshold = 0.3f;

    private Moveable _moveable;
    private NearestDetector _damageableDetector;
    private float _stateTimer;
    private bool _isMoving;
    private Vector2 _currentDirection;
    private Vector2 _collisionNormal;
    private bool _isCollidingAhead;

    #region IBehaviour Implementation
    public float Priority => 0f;

    public bool CanAct  => true;

    public bool IsBlocking => false;

    public void Sense()
    {
        // nothing to sense, this behavior is purely random movement
    }
    #endregion

    #region Unity Messages
    private void Awake()
    {
        this.TryInitComponent(ref _moveable);
        this.TryInitComponent(ref _damageableDetector, isOptional: true);
    }

    private void Start()
    {
        StartNewBehavior();
    }

    private void Update()
    {
        HandleRandomMovement();
    }
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!_isMoving || _currentDirection.sqrMagnitude < 0.01f)
            return;

        foreach (ContactPoint2D contact in collision.contacts)
        {
            Vector2 directionToCollision = -contact.normal;
            float dot = Vector2.Dot(_currentDirection.normalized, directionToCollision);

            if (dot > CollisionDirectionThreshold)
            {
                _collisionNormal = contact.normal;
                _isCollidingAhead = true;
                return;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        _isCollidingAhead = false;
    }

    private void OnDisable()
    {
        if (_moveable != null && _isMoving)
        {
            _moveable.Stop();
        }
        _isMoving = false;
        _isCollidingAhead = false;
        _stateTimer = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        if (_isMoving && _currentDirection.sqrMagnitude > 0.01f)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, _currentDirection);
        }
    }
    #endregion

    private void StartNewBehavior()
    {
        // If we hit something, bounce off it
        if (_collisionNormal.sqrMagnitude > 0.01f)
        {
            _currentDirection = Vector2.Reflect(_currentDirection, _collisionNormal).normalized;
            _collisionNormal = Vector2.zero;
            _moveable.Move(_currentDirection, run: false);
            _isMoving = true;
            _stateTimer = Random.Range(MinMoveDuration, MaxMoveDuration);
            return;
        }

        bool shouldOnlyFace = Random.value < OnlyFaceChance;

        if (shouldOnlyFace)
        {
            Vector2 newFacingDirection = GetRandomFacingDirection();
            _moveable.FaceDirection(newFacingDirection);
            _moveable.Stop();
            _isMoving = false;
            _currentDirection = Vector2.zero;
            _stateTimer = Random.Range(MinIdleDuration, MaxIdleDuration);
        }
        else
        {
            _currentDirection = GetRandomMovementDirection();
            _moveable.Move(_currentDirection, run: false);
            _isMoving = true;
            _stateTimer = Random.Range(MinMoveDuration, MaxMoveDuration);
        }
    }

    private Vector2 GetRandomMovementDirection()
    {
        Vector2[] directions = {
            Vector2.up,
            Vector2.down,
            Vector2.left,
            Vector2.right,
            new Vector2(1, 1).normalized,
            new Vector2(-1, 1).normalized,
            new Vector2(1, -1).normalized,
            new Vector2(-1, -1).normalized
        };
        return directions[Random.Range(0, directions.Length)];
    }

    private Vector2 GetRandomFacingDirection()
    {
        if (_moveable == null)
            return GetRandomCardinalDirection();

        return _moveable.CurrentFacingMode switch
        {
            FacingMode.HorizontalOnly => Random.value < 0.5f ? Vector2.left : Vector2.right,
            FacingMode.VerticalOnly => Random.value < 0.5f ? Vector2.up : Vector2.down,
            FacingMode.AllDirections => GetRandomCardinalDirection(),
            _ => GetRandomCardinalDirection()
        };
    }

    private Vector2 GetRandomCardinalDirection()
    {
        Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        return directions[Random.Range(0, directions.Length)];
    }

    private void HandleRandomMovement()
    {
        if (ShouldStopMoving())
        {
            _moveable.Stop();
            _isMoving = false;
            _isCollidingAhead = false;
            _stateTimer = Random.Range(MinIdleDuration, MaxIdleDuration);
            return;
        }

        /*_stateTimer = */_stateTimer.DecrementTimer();

        if (_stateTimer <= 0)
        {
            StartNewBehavior();
        }
    }

    private bool ShouldStopMoving()
    {
        if (!_isMoving)
            return false;

        if (IsDangerAhead())
            return true;

        if (_isCollidingAhead)
            return true;

        return false;
    }

    private bool IsDangerAhead()
    {
        if (_damageableDetector == null || !_damageableDetector.IsDetected)
            return false;

        if (!_damageableDetector.DirectionToTarget.HasValue)
            return false;

        float dot = Vector2.Dot(_currentDirection.normalized,
                                _damageableDetector.DirectionToTarget.Value);

        if (dot > DetectionConeThreshold)
        {
            // Use direction to danger as the "wall" to bounce off
            _collisionNormal = -_damageableDetector.DirectionToTarget.Value;
            return true;
        }

        return false;
    }

}
