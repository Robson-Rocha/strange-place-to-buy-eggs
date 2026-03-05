using System.Collections.Generic;
using UnityEngine;

public interface IBehaviour
{
    float Priority { get; }
    bool CanAct { get; }
    bool IsBlocking { get; }

    void Sense();

    public void Enable(bool enable) => (this as MonoBehaviour).enabled = enable;
}

public static class IBehaviourExtensions
{
    public static IBehaviour UpdateActiveBehaviour(this IReadOnlyList<IBehaviour> _behaviours)
    {
        int winner = -1;
        float highestPriority = float.MinValue;

        for (int i = 0; i < _behaviours.Count; i++)
        {
            var behaviour = _behaviours[i];
            behaviour.Sense();
            if (behaviour.CanAct && behaviour.Priority > highestPriority)
            {
                highestPriority = behaviour.Priority;
                winner = i;
            }
        }

        for (int i = 0; i < _behaviours.Count; i++)
        {
            _behaviours[i].Enable(i == winner);
        }

        return winner != -1 ? _behaviours[winner] : null;
    }
}