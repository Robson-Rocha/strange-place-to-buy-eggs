using System;
using UnityEngine;

/// <summary>
/// Defines a waypoint for patrol movement with optional pause duration.
/// </summary>
[Serializable]
public class PatrolWaypoint
{
    [Tooltip("Transform defining the waypoint position in world space.")]
    public Transform Position;

    [Tooltip("Minimum duration to pause at this waypoint before continuing.")]
    [Min(0f)] public float PauseDurationMin = 0f;

    [Tooltip("Maximum duration to pause at this waypoint before continuing.")]
    [Min(0f)] public float PauseDurationMax = 0f;

    public bool UseSimplePathfinding = false;

    [NonSerialized] public int RuntimeIndex = -1;

    /// <summary>
    /// Returns the world position of this waypoint, or null if Position is not set.
    /// </summary>
    public Vector2? WorldPosition => Position != null ? (Vector2)Position.position : null;

    /// <summary>
    /// Returns true if this waypoint has a valid position.
    /// </summary>
    public bool IsValid => Position != null;

    public void SetRuntimeMetadata(int runtimeIndex, Color gizmoColor)
    {
        RuntimeIndex = runtimeIndex;

        if (Position != null && Position.TryGetComponent(out WaypointGizmo waypointGizmo))
        {
            waypointGizmo.SetWaypointMetadata(displayIndex: runtimeIndex + 1, color: gizmoColor);
        }
    }
}
