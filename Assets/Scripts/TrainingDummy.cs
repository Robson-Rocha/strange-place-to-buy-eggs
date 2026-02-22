using RobsonRocha.UnityCommon;
using UnityEngine;

public class TrainingDummy : MonoBehaviour
{
    private AnimatorParameterBinder _animatorParameterBinder;
    private Damageable _damageable;

    private void Awake()
    {
        if (this.TryInitComponent(ref _damageable))
        {
            _damageable.TakingDamage += HandleDamageable_OnTakingDamage;
        }
        if (this.TryInitComponent(ref _animatorParameterBinder))
        {
            _animatorParameterBinder.BindTrigger(
                parameterName: "Hit", 
                behavior: AnimatorParameterBinder.BoolTriggerBehavior.ManualTrigger, 
                shouldRestartWhenRetrigger: true, 
                stateName: "Hit");
        }
    }

    private void HandleDamageable_OnTakingDamage(object sender, Damageable.TakingDamageEventArgs e)
    {
        _animatorParameterBinder.FireTrigger("Hit");
    }
}
