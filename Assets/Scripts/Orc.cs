using RobsonRocha.UnityCommon;
using UnityEngine;

[RequireComponent(typeof(Moveable))]
public class Orc : MonoBehaviour
{
    private AnimatorParameterBinder _animatorParameterBinder;
    private Moveable _moveable;
    private Knockbackable _knockbackable;

    private IBehaviour[] _behaviours;
    private IBehaviour _currentBehaviour;
    private IBehaviour CurrentBehaviour
    {
        get => _currentBehaviour;
        set
        {
            if (_currentBehaviour != value)
            {
                Debug.Log("Orc switched behaviour: " + (value != null ? value.GetType().Name : "null"));
            }
            _currentBehaviour = value;
        }
    }

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
        _behaviours = GetComponents<IBehaviour>();
    }

    // Update is called once per frame
    void Update()
    {
        if (CurrentBehaviour == null || !(CurrentBehaviour.CanAct && CurrentBehaviour.IsBlocking))
        {
            CurrentBehaviour = _behaviours.UpdateActiveBehaviour();
        }        
    }
}
