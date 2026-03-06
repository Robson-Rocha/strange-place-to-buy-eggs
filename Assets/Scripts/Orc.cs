using RobsonRocha.UnityCommon;
using UnityEngine;

[RequireComponent(typeof(Moveable))]
public class Orc : MonoBehaviour
{
    private AnimatorParameterBinder _animatorParameterBinder;
    private Moveable _moveable;
    private Knockbackable _knockbackable;

    private IAiBehaviour[] _behaviours;
    private IAiBehaviour _currentBehaviour;
    private IAiBehaviour CurrentBehaviour
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
        _behaviours = GetComponents<IAiBehaviour>();
    }

    // Update is called once per frame
    void Update()
    {
        HandleAiBehaviours();
    }

    public void HandleAiBehaviours()
    {
        foreach (var behaviour in _behaviours) {
            behaviour.HeartBeat();
        }

        if (CurrentBehaviour != null)
        {
            CurrentBehaviour.Sense();
            if (CurrentBehaviour.IsBlocking && CurrentBehaviour.CanAct)
            {
                return;
            }
        }

        CurrentBehaviour = _behaviours.UpdateActiveAiBehaviour(skip: CurrentBehaviour);
    }

}
