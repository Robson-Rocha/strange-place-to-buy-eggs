using UnityEngine;

public abstract class AiBehaviourBase : MonoBehaviour, IAiBehaviour
{
    public virtual int Priority { get; protected set; }
    public virtual bool CanAct { get; protected set; }
    public virtual bool IsBlocking { get; protected set; }

    public virtual void HeartBeat() { }

    public virtual void Sense() { }

    public virtual void Awake()
    {
        this.enabled = false; // Start disabled by default, will be enabled by the AI manager when selected as active behaviour
    }
}
