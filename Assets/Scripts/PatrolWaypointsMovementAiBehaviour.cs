using System.Collections.Generic;
using RobsonRocha.UnityCommon;
using UnityEngine;

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

/// <summary>
/// AI behaviour that moves an NPC along a defined set of waypoints.
/// Supports multiple traversal patterns and recovery strategies.
/// </summary>
[RequireComponent(typeof(Moveable))]
[DefaultExecutionOrder(10)]
public class PatrolWaypointsMovementAiBehaviour : AiBehaviourBase
{
    private const float MINIMUM_ARRIVAL_THRESHOLD = 0.01f;
    private const float GIZMO_ARROW_LENGTH = 0.2f;
    private const float GIZMO_ARROW_WIDTH = 0.08f;

    [Header("Behaviour Settings")]
    [SerializeField][Range(-100, 100)] private int BehaviourPriority = -100;
    [SerializeField] private bool IsDisabled = false;

    [Header("Patrol Settings")]
    [SerializeField] private PatrolPattern Pattern = PatrolPattern.Cycle;
    [SerializeField] private PatrolRecoveryStrategy RecoveryStrategy = PatrolRecoveryStrategy.Continue;
    [SerializeField] private List<PatrolWaypoint> Waypoints = new();

    [Header("Movement Settings")]
    [SerializeField][Min(0f)] private float ArrivalThreshold = 0.1f;
    [SerializeField] private bool RunToWaypoints = false;

    [SerializeField][HideInInspector] private Color WaypointsGizmoColor = default;

    private Moveable _moveable;
    private IPathfinder _pathfinder;
    private IPathfinder _simplePathfinder;

    // Pattern state
    private int _currentWaypointIndex;
    private int _patternDirection = 1; // 1 = forward, -1 = backward (for Pendulum)
    private HashSet<int> _visitedThisCycle = new();

    // Route state
    private Vector2[] _currentRoute;
    private int _currentRouteIndex;

    // Pause state
    private float _pauseTimer;
    private bool _isPaused;

    // Recovery state
    private bool _needsRecovery;
    private Vector2 _lastKnownPosition;

    #region AI Behaviour Implementation
    public override int Priority => BehaviourPriority;

    public override bool IsBlocking => false;

    public override void HeartBeat()
    {
        if (!IsDisabled && _isPaused)
        {
            _pauseTimer.DecrementTimer();
            if (_pauseTimer <= 0f)
            {
                _isPaused = false;
                AdvanceToNextWaypoint();
            }
        }
    }

    public override void Sense()
    {
        CanAct = !IsDisabled && !_isPaused && HasValidWaypoints();
    }
    #endregion

    #region Unity Messages
    public override void Awake()
    {
        base.Awake();
        this.TryInitComponent(ref _moveable);
        _pathfinder = PathfinderManager.Instance;
        _simplePathfinder = SimplePathfinderManager.Instance;
        if (_pathfinder == null)
        {
            Debug.LogError($"{nameof(PatrolWaypointsMovementAiBehaviour)}: {nameof(PathfinderManager)} not found in scene!", this);
            IsDisabled = false;
            return;
        }
        if (_simplePathfinder == null)
        {
            Debug.LogError($"{nameof(PatrolWaypointsMovementAiBehaviour)}: {nameof(SimplePathfinderManager)} not found in scene!", this);
            IsDisabled = false;
            return;
        }
        HandleInitializeWaypointsGizmoColor();
        HandleSyncWaypointMetadata();
    }

    private void Start()
    {
        InitializePatrol();
    }

    private void Update()
    {
        HandlePatrolMovement();
    }

    private void OnEnable()
    {
        // Check if we need recovery (were we displaced?)
        if (_lastKnownPosition != Vector2.zero)
        {
            float displacement = ((Vector2)transform.position - _lastKnownPosition).sqrMagnitude;
            if (displacement > ArrivalThreshold * ArrivalThreshold)
            {
                _needsRecovery = true;
            }
        }
    }

    private void OnDisable()
    {
        if (_moveable != null)
        {
            _moveable.Stop();
        }
        _lastKnownPosition = transform.position;
    }
    #endregion

    private void HandleInitializeWaypointsGizmoColor()
    {
        if (WaypointsGizmoColor.a > 0f)
            return;

        float hue = Mathf.Abs(GetInstanceID() * 0.61803398875f);
        hue -= Mathf.Floor(hue);
        WaypointsGizmoColor = Color.HSVToRGB(hue, 0.8f, 1f);
        WaypointsGizmoColor.a = 1f;
    }

    private void HandleSyncWaypointMetadata()
    {
        HandleInitializeWaypointsGizmoColor();

        if (Waypoints == null)
            return;

        for (int i = 0; i < Waypoints.Count; i++)
        {
            Waypoints[i]?.SetRuntimeMetadata(i, WaypointsGizmoColor);
        }
    }

    private bool HasValidWaypoints()
    {
        if (Waypoints == null || Waypoints.Count == 0)
            return false;

        for (int i = 0; i < Waypoints.Count; i++)
        {
            if (Waypoints[i] != null && Waypoints[i].IsValid)
                return true;
        }

        return false;
    }

    private void AdvanceToNextWaypoint()
    {
        int nextIndex = GetNextWaypointIndex();

        // Check if we've completed a cycle
        if (IsCycleComplete(nextIndex))
        {
            _visitedThisCycle.Clear();
        }

        _currentWaypointIndex = nextIndex;
        CalculateRouteToCurrentWaypoint();
    }

    private int GetNextWaypointIndex()
    {
        return Pattern switch
        {
            PatrolPattern.Pendulum => GetNextPendulumIndex(),
            PatrolPattern.Cycle => GetNextCycleIndex(),
            PatrolPattern.Random => GetNextRandomIndex(),
            _ => GetNextCycleIndex()
        };
    }

    private int GetNextPendulumIndex()
    {
        int nextIndex = _currentWaypointIndex + _patternDirection;

        if (nextIndex >= Waypoints.Count)
        {
            _patternDirection = -1;
            nextIndex = _currentWaypointIndex + _patternDirection;
        }
        else if (nextIndex < 0)
        {
            _patternDirection = 1;
            nextIndex = _currentWaypointIndex + _patternDirection;
        }

        return Mathf.Clamp(nextIndex, 0, Waypoints.Count - 1);
    }

    private int GetNextCycleIndex()
    {
        return (_currentWaypointIndex + 1) % Waypoints.Count;
    }

    private int GetNextRandomIndex()
    {
        if (Waypoints.Count == 1)
            return 0;

        int nextIndex;
        do
        {
            nextIndex = Random.Range(0, Waypoints.Count);
        } while (nextIndex == _currentWaypointIndex);

        return nextIndex;
    }

    private bool IsCycleComplete(int nextIndex)
    {
        return Pattern switch
        {
            PatrolPattern.Pendulum => nextIndex == 0 && _patternDirection == 1,
            PatrolPattern.Cycle => nextIndex == 0,
            PatrolPattern.Random => false, // Random never completes a "cycle"
            _ => false
        };
    }

    private void CalculateRouteToCurrentWaypoint()
    {
        if (!HasValidWaypoints())
        {
            _currentRoute = null;
            return;
        }

        var waypoint = Waypoints[_currentWaypointIndex];
        if (!waypoint.IsValid)
        {
            _currentRoute = null;
            return;
        }

        IPathfinder pathfinder = waypoint.UseSimplePathfinding ? _simplePathfinder : _pathfinder;
        if (pathfinder == null)
        {
            _currentRoute = null;
            return;
        }

        Vector2 currentPos = transform.position;
        Vector2 targetPos = waypoint.WorldPosition.Value;

        _currentRoute = pathfinder.GetRouteToTarget(currentPos, targetPos, ignoreOwner: transform);
        _currentRouteIndex = 0;
    }

    private void InitializePatrol()
    {
        if (!HasValidWaypoints())
            return;

        _currentWaypointIndex = 0;
        _patternDirection = 1;
        _visitedThisCycle.Clear();
        _currentRoute = null;
        _currentRouteIndex = 0;
        _isPaused = false;
        _pauseTimer = 0f;
        _needsRecovery = false;

        CalculateRouteToCurrentWaypoint();
    }

    private void HandlePatrolMovement()
    {
        if (!HasValidWaypoints() || _moveable == null)
            return;

        if (_isPaused)
            return;

        if (_needsRecovery)
        {
            HandleRecovery();
            _needsRecovery = false;
        }

        if (_currentRoute == null || _currentRoute.Length == 0)
        {
            CalculateRouteToCurrentWaypoint();
            if (_currentRoute == null || _currentRoute.Length == 0)
                return;
        }

        Vector2 currentPosition = transform.position;
        Vector2 targetPosition = _currentRoute[_currentRouteIndex];
        Vector2 direction = (targetPosition - currentPosition);
        float distanceSqr = direction.sqrMagnitude;
        float thresholdSqr = Mathf.Max(ArrivalThreshold, MINIMUM_ARRIVAL_THRESHOLD);
        thresholdSqr *= thresholdSqr;

        if (distanceSqr <= thresholdSqr)
        {
            HandleArrivalAtRouteNode();
        }
        else
        {
            _moveable.Move(direction.normalized, RunToWaypoints);
        }

        _lastKnownPosition = currentPosition;
    }

    private void HandleRecovery()
    {
        switch (RecoveryStrategy)
        {
            case PatrolRecoveryStrategy.Continue:
                // Just recalculate route to current target
                CalculateRouteToCurrentWaypoint();
                break;

            case PatrolRecoveryStrategy.NearestVisited:
                RecoverToNearestWaypoint(visitedOnly: true);
                break;

            case PatrolRecoveryStrategy.Nearest:
                RecoverToNearestWaypoint(visitedOnly: false);
                break;
        }
    }

    private void RecoverToNearestWaypoint(bool visitedOnly)
    {
        var candidatePositions = new List<Vector2>();
        var candidateIndices = new List<int>();
        bool useSimplePathfinding = false;

        for (int i = 0; i < Waypoints.Count; i++)
        {
            if (!Waypoints[i].IsValid)
                continue;

            if (visitedOnly && !_visitedThisCycle.Contains(i))
                continue;

            candidatePositions.Add(Waypoints[i].WorldPosition.Value);
            candidateIndices.Add(i);

            // Use simple pathfinding if the current target waypoint uses it
            if (i == _currentWaypointIndex)
            {
                useSimplePathfinding = Waypoints[i].UseSimplePathfinding;
            }
        }

        // If no visited waypoints, fall back to Continue strategy
        if (candidatePositions.Count == 0)
        {
            CalculateRouteToCurrentWaypoint();
            return;
        }

        IPathfinder pathfinder = useSimplePathfinding ? _simplePathfinder : _pathfinder;
        if (pathfinder == null)
            return;

        Vector2 currentPos = transform.position;
        Vector2[] route = pathfinder.GetRouteToNearestTarget(currentPos, candidatePositions, ignoreOwner: transform);

        if (route.Length > 0)
        {
            // Find which waypoint index this route leads to
            Vector2 targetPos = route[route.Length - 1];
            for (int i = 0; i < candidatePositions.Count; i++)
            {
                if ((candidatePositions[i] - targetPos).sqrMagnitude < 0.001f)
                {
                    _currentWaypointIndex = candidateIndices[i];
                    break;
                }
            }

            _currentRoute = route;
            _currentRouteIndex = 0;
        }
    }

    private void HandleArrivalAtRouteNode()
    {
        _currentRouteIndex++;

        // If we've completed the current route, we've arrived at the waypoint
        if (_currentRouteIndex >= _currentRoute.Length)
        {
            HandleArrivalAtWaypoint();
        }
    }

    private void HandleArrivalAtWaypoint()
    {
        _visitedThisCycle.Add(_currentWaypointIndex);

        var waypoint = Waypoints[_currentWaypointIndex];
        float pauseDurationMin = waypoint.PauseDurationMin;
        float pauseDurationMax = waypoint.PauseDurationMax;

        if (pauseDurationMin.IsNearZero() && pauseDurationMax.IsNearZero())
        {
            AdvanceToNextWaypoint();
            return;
        }

        float minPause = Mathf.Min(pauseDurationMin, pauseDurationMax);
        float maxPause = Mathf.Max(pauseDurationMin, pauseDurationMax);

        _moveable.Stop();
        _pauseTimer = Mathf.Approximately(minPause, maxPause)
            ? minPause
            : Random.Range(minPause, maxPause);
        _isPaused = true;
    }

    private bool TryGetNextGizmoConnectionIndex(int currentIndex, out int nextIndex)
    {
        nextIndex = -1;

        if (Waypoints == null || Waypoints.Count < 2)
            return false;

        switch (Pattern)
        {
            case PatrolPattern.Random:
                return false;

            case PatrolPattern.Pendulum:
                if (currentIndex >= Waypoints.Count - 1)
                    return false;
                nextIndex = currentIndex + 1;
                return true;

            case PatrolPattern.Cycle:
                nextIndex = (currentIndex + 1) % Waypoints.Count;
                return true;

            default:
                return false;
        }
    }

    private void DrawGizmoDirectionArrow(Vector3 from, Vector3 to)
    {
        Vector3 direction = to - from;
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        Vector3 forward = direction.normalized;
        Vector3 tip = to;
        Vector3 basePoint = tip - (forward * GIZMO_ARROW_LENGTH);
        Vector3 perpendicular = new Vector3(-forward.y, forward.x, 0f);

        Vector3 leftPoint = basePoint + (perpendicular * GIZMO_ARROW_WIDTH);
        Vector3 rightPoint = basePoint - (perpendicular * GIZMO_ARROW_WIDTH);

        Gizmos.DrawLine(tip, leftPoint);
        Gizmos.DrawLine(tip, rightPoint);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        HandleSyncWaypointMetadata();
    }
#endif

    private void OnDrawGizmosSelected()
    {
        if (Waypoints == null || Waypoints.Count == 0)
            return;

        Color previousColor = Gizmos.color;
        Gizmos.color = WaypointsGizmoColor.a > 0f ? WaypointsGizmoColor : Color.cyan;

        for (int i = 0; i < Waypoints.Count; i++)
        {
            if (Waypoints[i]?.Position == null)
                continue;

            Vector3 currentPosition = Waypoints[i].Position.position;
            Gizmos.DrawWireSphere(currentPosition, 0.2f);

            if (!TryGetNextGizmoConnectionIndex(i, out int nextIndex))
                continue;

            if (Waypoints[nextIndex]?.Position == null)
                continue;

            Vector3 nextPosition = Waypoints[nextIndex].Position.position;
            Gizmos.DrawLine(currentPosition, nextPosition);
            DrawGizmoDirectionArrow(currentPosition, nextPosition);
        }

        Gizmos.color = previousColor;
    }
}
