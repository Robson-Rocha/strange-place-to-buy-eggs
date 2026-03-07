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

    [Header("Random Area Settings")]
    [Tooltip("If enabled, generates random positions within AreaSize instead of using Position directly.")]
    public bool UseRandomArea = false;

    [Tooltip("Size of the rectangular patrol area centered on Position (only used if UseRandomArea is enabled).")]
    [Min(0.1f)] public Vector2 AreaSize = new Vector2(5f, 5f);

    [NonSerialized] public int RuntimeIndex = -1;
    [NonSerialized] private Vector2? _currentRandomPosition;

    /// <summary>
    /// Returns the world position of this waypoint.
    /// If UseRandomArea is enabled, returns the current random position within the area.
    /// Otherwise, returns the Position transform's position, or null if Position is not set.
    /// </summary>
    public Vector2? WorldPosition
    {
        get
        {
            if (UseRandomArea)
                return _currentRandomPosition;

            return Position != null ? (Vector2)Position.position : null;
        }
    }

    /// <summary>
    /// Returns true if this waypoint has a valid position.
    /// </summary>
    public bool IsValid
    {
        get
        {
            if (Position == null)
                return false;

            if (UseRandomArea)
                return AreaSize.x > 0f && AreaSize.y > 0f;

            return true;
        }
    }

    /// <summary>
    /// Called when this waypoint is selected as the next target.
    /// If UseRandomArea is enabled, generates a new random position within the area.
    /// </summary>
    public void OnSelected()
    {
        if (!UseRandomArea || Position == null)
        {
            _currentRandomPosition = null;
            return;
        }

        Vector2 center = Position.position;
        Vector2 randomOffset = new Vector2(
            UnityEngine.Random.Range(-AreaSize.x / 2f, AreaSize.x / 2f),
            UnityEngine.Random.Range(-AreaSize.y / 2f, AreaSize.y / 2f)
        );

        _currentRandomPosition = center + randomOffset;
    }

    public void SetRuntimeMetadata(int runtimeIndex, Color gizmoColor)
    {
        RuntimeIndex = runtimeIndex;

        if (Position != null && Position.TryGetComponent(out WaypointGizmo waypointGizmo))
        {
            waypointGizmo.SetWaypointMetadata(displayIndex: runtimeIndex + 1, color: gizmoColor);
        }
    }
}
