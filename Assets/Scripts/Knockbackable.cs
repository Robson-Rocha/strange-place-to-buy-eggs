using RobsonRocha.UnityCommon;
using UnityEngine;

/// <summary>
/// Component that handles knockback logic for characters.
/// Can be used on players, enemies, or any entity that can be knocked back.
/// Automatically subscribes to Damageable.TakingDamage if a Damageable component is present.
/// </summary>
public class Knockbackable : MonoBehaviour
{
    [Header("Knockback Settings")]
    [Tooltip("Default force applied during knockback")]
    [SerializeField] private float DefaultForce = 8f;

    [Tooltip("Default duration of the knockback effect in seconds")]
    [SerializeField] private float DefaultDuration = 0.3f;

    [Tooltip("Easing curve for knockback velocity (typically ease-out: starts fast, slows down)")]
    [SerializeField] private AnimationCurve EasingCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    private Damageable _damageable;
    private Vector2 _knockbackDirection;
    private Vector2 _facingDirection;
    private float _currentForce;
    private float _currentDuration;
    private float _elapsedTime;
    private bool _isActive;

    /// <summary>
    /// Returns true while the knockback effect is active.
    /// Use this to block player/enemy input during knockback.
    /// </summary>
    public bool IsKnockingBack => _isActive;

    /// <summary>
    /// Returns the direction the character should face during knockback (towards the attacker).
    /// </summary>
    public Vector2 FacingDirection => _facingDirection;

    /// <summary>
    /// Starts a knockback effect from the given source position.
    /// </summary>
    /// <param name="sourcePosition">The world position of the damage source (attacker)</param>
    /// <param name="force">Optional override for knockback force. If null, uses default.</param>
    /// <param name="duration">Optional override for knockback duration. If null, uses default.</param>
    public void StartKnockback(Vector3 sourcePosition, float? force = null, float? duration = null)
    {
        // Calculate direction from source to this object (knockback direction is away from source)
        Vector2 toTarget = (Vector2)(transform.position - sourcePosition);
        _knockbackDirection = toTarget.SnapToAngle(90f);

        // Facing direction is towards the source (the attacker)
        _facingDirection = -_knockbackDirection;

        _currentForce = force ?? DefaultForce;
        _currentDuration = duration ?? DefaultDuration;
        _elapsedTime = 0f;
        _isActive = true;
    }

    /// <summary>
    /// Stops the knockback effect immediately.
    /// </summary>
    public void StopKnockback()
    {
        _isActive = false;
        _elapsedTime = 0f;
    }

    /// <summary>
    /// Returns the current knockback velocity for this frame.
    /// Call this every frame while IsActive is true to get the eased velocity.
    /// </summary>
    /// <returns>The velocity to apply for knockback movement</returns>
    public Vector2 GetCurrentVelocity()
    {
        if (!_isActive)
            return Vector2.zero;

        // Calculate normalized time (0 to 1)
        float normalizedTime = Mathf.Clamp01(_elapsedTime / _currentDuration);

        // Evaluate the easing curve to get the current force multiplier
        float easedMultiplier = EasingCurve.Evaluate(normalizedTime);

        // Return the knockback velocity
        return _currentForce * easedMultiplier * _knockbackDirection;
    }

    private void Awake()
    {
        // Auto-wire to Damageable if present
        if (this.TryInitComponent(ref _damageable, isOptional: true))
        {
            _damageable.TakingDamage += HandleDamageable_OnTakingDamage;
        }
    }

    private void Update()
    {
        if (!_isActive)
            return;

        _elapsedTime += Time.deltaTime;

        if (_elapsedTime >= _currentDuration)
        {
            StopKnockback();
        }
    }

    private void OnDestroy()
    {
        if (_damageable != null)
        {
            _damageable.TakingDamage -= HandleDamageable_OnTakingDamage;
        }
    }

    private void HandleDamageable_OnTakingDamage(object sender, Damageable.TakingDamageEventArgs e)
    {
        StartKnockback(e.SourcePosition);
    }
}
