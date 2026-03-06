using RobsonRocha.UnityCommon;
using UnityEngine;

/// <summary>
/// Behaviour that takes control of movement during knockback.
/// Has high priority and blocks other behaviours while active.
/// </summary>
[RequireComponent(typeof(Moveable))]
[RequireComponent(typeof(Knockbackable))]
[DefaultExecutionOrder(10)]
public class KnockbackMovementAiBehaviour : AiBehaviourBase
{
    [Header("Behaviour Settings")]
    [SerializeField][Range(-100, 100)] private int BehaviourPriority = 100;
    [SerializeField] private bool IsDisabled = false;

    private Moveable _moveable;
    private Knockbackable _knockbackable;

    #region AI Behaviour Implementation
    public override int Priority => BehaviourPriority;

    public override bool CanAct { get; protected set; }

    public override bool IsBlocking => true;
    public override void Sense()
    {
        CanAct = !IsDisabled &&
                _knockbackable != null &&
                _knockbackable.IsKnockingBack;
    }
    #endregion

    #region Unity Messages
    public override void Awake()
    {
        base.Awake();
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
