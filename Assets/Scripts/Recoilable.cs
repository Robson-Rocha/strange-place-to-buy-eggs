using RobsonRocha.UnityCommon;
using UnityEngine;

/// <summary>
/// Component that handles recoil logic for attackers.
/// Applies a short pushback when hitting a target, without blocking movement or animations.
/// Typically added to entities that can deal damage (Player, enemies with attacks).
/// </summary>
public class Recoilable : MonoBehaviour
{
    [Header("Recoil Settings")]
    [Tooltip("Force applied during recoil")]
    [SerializeField] private float RecoilForce = 4f;

    [Tooltip("Duration of the recoil effect in seconds")]
    [SerializeField] private float RecoilDuration = 0.1f;

    [Tooltip("Easing curve for recoil velocity")]
    [SerializeField] private AnimationCurve EasingCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    private Vector2 _recoilDirection;
    private float _elapsedTime;
    private float _currentForce;
    private float _currentDuration;
    private bool _isActive;

    /// <summary>
    /// Returns true while the recoil effect is active.
    /// Unlike knockback, this does NOT block other actions.
    /// </summary>
    public bool IsRecoiling => _isActive;

    /// <summary>
    /// Applies recoil away from the target position.
    /// </summary>
    public void ApplyRecoil(Vector3 targetPosition, float? force = null, float? duration = null)
    {
        Vector2 awayFromTarget = (Vector2)(transform.position - targetPosition);
        _recoilDirection = awayFromTarget.SnapToAngle(90f);

        _elapsedTime = 0f;
        _isActive = true;
        _currentForce = force ?? RecoilForce;
        _currentDuration = duration ?? RecoilDuration;
    }

    /// <summary>
    /// Returns the current recoil velocity to add to movement.
    /// </summary>
    public Vector2 GetCurrentVelocity()
    {
        if (!_isActive)
            return Vector2.zero;

        float normalizedTime = Mathf.Clamp01(_elapsedTime / _currentDuration);
        float easedMultiplier = EasingCurve.Evaluate(normalizedTime);

        return _currentForce * easedMultiplier * _recoilDirection;
    }

    private void Update()
    {
        if (!_isActive)
            return;

        _elapsedTime += Time.deltaTime;

        if (_elapsedTime >= _currentDuration)
        {
            _isActive = false;
        }
    }
}