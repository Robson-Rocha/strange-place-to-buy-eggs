
/// <summary>
/// Strategy for recovering to patrol route after displacement.
/// </summary>
public enum PatrolRecoveryStrategy
{
    /// <summary>Continue to the waypoint we were heading to before displacement.</summary>
    Continue,
    /// <summary>Go to the nearest waypoint that was already visited this cycle.</summary>
    NearestVisited,
    /// <summary>Go to the absolutely nearest waypoint (may skip unvisited waypoints).</summary>
    Nearest
}
