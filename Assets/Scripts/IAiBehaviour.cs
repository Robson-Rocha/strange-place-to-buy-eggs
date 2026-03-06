using System.Collections.Generic;
using UnityEngine;

public interface IAiBehaviour
{
    int Priority { get; }
    bool CanAct { get; }
    bool IsBlocking { get; }

    public void HeartBeat() 
    {
        // This method can be called when there are actions to be performed even if the behaviour cannot sense this frame
        // For example, to update internal timers or cooldowns that don't necessarily require sensing every frame
    }

    public void Sense()
    {
        // This method should be called every frame to allow the behaviour to update its CanAct and IsBlocking properties based on the current game state
        // Some behaviors may not need to sense, simply acting if there are no other higher priority behaviors active, but they should still implement this method to update their internal state if necessary
    }

    public void Enable(bool enable) => (this as MonoBehaviour).enabled = enable;
}

public static class IAiBehaviourExtensions
{
    public static IAiBehaviour UpdateActiveAiBehaviour(this IReadOnlyList<IAiBehaviour> _behaviours, IAiBehaviour skip = null)
    {
        int winner = -1;
        int highestPriority = int.MinValue;

        for (int i = 0; i < _behaviours.Count; i++)
        {
            var behaviour = _behaviours[i];
            if (behaviour != skip)
            {
                behaviour.Sense();
            }
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