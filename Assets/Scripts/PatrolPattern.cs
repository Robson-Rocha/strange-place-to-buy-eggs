
/// <summary>
/// Pattern for traversing patrol waypoints.
/// </summary>
public enum PatrolPattern
{
    /// <summary>A → B → C → B → A → B...</summary>
    Pendulum,
    /// <summary>A → B → C → A → B → C...</summary>
    Cycle,
    /// <summary>Randomly selects next waypoint each time.</summary>
    Random
}
