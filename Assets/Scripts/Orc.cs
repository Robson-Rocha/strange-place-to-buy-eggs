using RobsonRocha.UnityCommon;
using UnityEngine;

[RequireComponent(typeof(Moveable))]
[RequireComponent(typeof(AnimatorParameterBinder))]
[RequireComponent(typeof(Knockbackable))]
[RequireComponent(typeof(FieldOfViewDetector))]
[RequireComponent(typeof(AiBehaviourController))]
public class Orc : MonoBehaviour
{
    private AnimatorParameterBinder _animatorParameterBinder;
    private Moveable _moveable;
    private Knockbackable _knockbackable;
    private FieldOfViewDetector _playerFieldOfViewDetector;

    void Awake()
    {
        if (this.TryInitComponent(ref _moveable) &&
            this.TryInitComponent(ref _knockbackable) &&
            this.TryInitComponent(ref _animatorParameterBinder))
        {
            _animatorParameterBinder.Bind(Consts.ANIM_PARAM_IS_RUNNING, () => !_knockbackable.IsKnockingBack && _moveable.IsRunning);
            _animatorParameterBinder.Bind(Consts.ANIM_PARAM_IS_IDLING, () => !_knockbackable.IsKnockingBack && _moveable.IsIdling);
            _animatorParameterBinder.Bind(Consts.ANIM_PARAM_IS_FACING_LEFT, () => _moveable.IsFacingLeft);
            _animatorParameterBinder.Bind(Consts.ANIM_PARAM_IS_FACING_RIGHT, () => _moveable.IsFacingRight);
            _animatorParameterBinder.Bind(Consts.ANIM_PARAM_IS_KNOCKING_BACK, () => _knockbackable.IsKnockingBack);
        }

        if (_moveable != null &&
            this.TryInitComponent(ref _playerFieldOfViewDetector))
        {
            _playerFieldOfViewDetector.GetFacingDirection = () => _moveable.FacingDirection;
        }
    }
}
