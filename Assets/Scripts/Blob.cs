using RobsonRocha.UnityCommon;
using UnityEngine;

[RequireComponent(typeof(Moveable))]
[RequireComponent(typeof(AnimatorParameterBinder))]
[RequireComponent(typeof(Knockbackable))]
[RequireComponent(typeof(AiBehaviourController))]
public class Blob : MonoBehaviour
{
    private AnimatorParameterBinder _animatorParameterBinder;
    private Moveable _moveable;
    private Knockbackable _knockbackable;

    void Awake()
    {
        if (this.TryInitComponent(ref _moveable) &&
            this.TryInitComponent(ref _knockbackable) &&
            this.TryInitComponent(ref _animatorParameterBinder))
        {
            _animatorParameterBinder.Bind(Consts.ANIM_PARAM_IS_MOVING, () => !_knockbackable.IsKnockingBack && _moveable.IsMoving);
            _animatorParameterBinder.Bind(Consts.ANIM_PARAM_IS_IDLING, () => !_knockbackable.IsKnockingBack && _moveable.IsIdling);
            _animatorParameterBinder.Bind(Consts.ANIM_PARAM_IS_FACING_LEFT, () => _moveable.IsFacingLeft);
            _animatorParameterBinder.Bind(Consts.ANIM_PARAM_IS_FACING_RIGHT, () => _moveable.IsFacingRight);
            _animatorParameterBinder.Bind(Consts.ANIM_PARAM_IS_KNOCKING_BACK, () => _knockbackable.IsKnockingBack);
        }
    }
}
