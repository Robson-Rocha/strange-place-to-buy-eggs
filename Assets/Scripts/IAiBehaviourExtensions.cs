using System.Collections.Generic;

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