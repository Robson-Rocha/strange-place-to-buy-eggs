using UnityEngine;

/// <summary>
/// Coordinates AI behaviours on an entity, managing their priorities and execution.
/// </summary>
public class AiBehaviourController : MonoBehaviour
{
    private IAiBehaviour[] _behaviours;
    private IAiBehaviour _currentBehaviour;
    
    private IAiBehaviour CurrentBehaviour
    {
        get => _currentBehaviour;
        set
        {
            if (_currentBehaviour != value)
            {
                Debug.Log($"{gameObject.name} switched behaviour: {(value != null ? value.GetType().Name : "null")}");
            }
            _currentBehaviour = value;
        }
    }

    void Awake()
    {
        _behaviours = GetComponents<IAiBehaviour>();
    }

    void Update()
    {
        HandleAiBehaviours();
    }

    private void HandleAiBehaviours()
    {
        foreach (var behaviour in _behaviours)
        {
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