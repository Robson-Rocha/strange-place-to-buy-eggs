using RobsonRocha.UnityCommon;
using UnityEngine;

/// <summary>
/// Behaviour that takes control of movement during knockback.
/// Has high priority and blocks other behaviours while active.
/// </summary>
[RequireComponent(typeof(Moveable))]
[RequireComponent(typeof(Knockbackable))]
[DefaultExecutionOrder(10)]
public class KnockbackMovementBehaviour : MonoBehaviour, IBehaviour
{
    [Header("Behaviour Settings")]
    [SerializeField] private float BehaviourPriority = 100f;

    private Moveable _moveable;
    private Knockbackable _knockbackable;

    #region IBehaviour Implementation
    public float Priority => BehaviourPriority;

    public bool CanAct => _knockbackable != null && _knockbackable.IsKnockingBack;

    public bool IsBlocking => CanAct;

    public void Sense()
    {
        // Nothing to sense - knockback state is managed by Knockbackable
    }
    #endregion

    #region Unity Messages
    private void Awake()
    {
        this.TryInitComponent(ref _moveable);
        this.TryInitComponent(ref _knockbackable);
    }

    private void Update()
    {
        HandleKnockbackMovement();
    }

    private void OnDisable()
    {
        if (_moveable != null)
        {
            _moveable.ClearExternalVelocity();
        }
    }
    #endregion

    private void HandleKnockbackMovement()
    {
        if (_knockbackable == null || _moveable == null)
            return;

        if (_knockbackable.IsKnockingBack)
        {
            _moveable.SetExternalVelocity(
                _knockbackable.GetCurrentVelocity(),
                _knockbackable.FacingDirection);
        }
        else
        {
            _moveable.ClearExternalVelocity();
        }
    }
}
