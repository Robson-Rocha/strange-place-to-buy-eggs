using UnityEngine;

public interface IAiBehaviour
{
    int Priority { get; }
    bool CanAct { get; }
    bool IsBlocking { get; }

    // This method can be called when there are actions to be performed even if the behaviour cannot sense this frame
    // For example, to update internal timers or cooldowns that don't necessarily require sensing every frame
    void HeartBeat();

    // This method should be called every frame to allow the behaviour to update its CanAct and IsBlocking properties based on the current game state
    // Some behaviors may not need to sense, simply acting if there are no other higher priority behaviors active, but they should still implement this method to update their internal state if necessary
    void Sense();

    public void Enable(bool enable) => (this as MonoBehaviour).enabled = enable;
}
