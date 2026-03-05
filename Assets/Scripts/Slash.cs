using UnityEngine;

public class Slash : Damaging, IEmittable
{
    private Recoilable _recoilable;

    public void OnEmit(GameObject emitter)
    {
        if (emitter != null)
        {
            emitter.TryGetComponent(out _recoilable);
        }
    }

    public override void DoDamage(Damageable damageable)
    {
        base.DoDamage(damageable);

        if (_recoilable != null)
        {
            _recoilable.ApplyRecoil(damageable.transform.position);
        }
    }
}


