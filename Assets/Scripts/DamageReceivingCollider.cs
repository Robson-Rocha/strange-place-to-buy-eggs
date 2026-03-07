using System;
using UnityEngine;

[Serializable]
public class DamageReceivingCollider
{
    public Collider2D Collider;
    public string KindOfDamageReceived = null;
}
