using UnityEngine;

public class Damaging : MonoBehaviour
{
    public int AmountOfDamageCaused = 1;

    public string KindOfDamageCaused = null;

    public virtual void DoDamage(Damageable damageable)
    {
        damageable.TakeDamage(AmountOfDamageCaused, transform.position, KindOfDamageCaused);
    }
}
