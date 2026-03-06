using RobsonRocha.UnityCommon;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Where within each grid node the waypoint coordinates should be placed.
/// </summary>
public enum WaypointAlignment
{
    Center,
    Left,
    Right,
    Top,
    Bottom
}

/// <summary>
/// Singleton service for A* pathfinding operations on tilemap-based grids.
/// Supports dynamic obstacle detection and caching.
/// </summary>
[DefaultExecutionOrder(-10)]
public class PathfinderManager : SingletonMonoBehaviour<PathfinderManager>, IPathfinder
{
    private class PathfinderNode
    {
        public Vector2Int GridPosition;
        public Vector2 WorldPosition;
        public bool IsTilemapWalkable;       // Original from tilemaps (immutable after init)
        public bool IsDamaging;              // Runtime damaging detection (from Damaging components)
        public bool IsNearDamaging;          // Within DistanceFromDamagings radius of a damaging node
        public HashSet<Transform> DynamicBlockers = new(); // Transforms blocking this node dynamically

        public bool IsWalkable => IsTilemapWalkable && DynamicBlockers.Count == 0;

        // A* working data (reset per pathfinding request)
        public float GCost;
        public float HCost;
        public float FCost => GCost + HCost;
        public PathfinderNode Parent;

        public void ResetPathfindingData()
        {
            GCost = float.MaxValue;
            HCost = 0f;
            Parent = null;
        }

        public bool IsWalkableIgnoring(Transform ignoreOwner)
        {
            if (!IsTilemapWalkable)
                return false;

            if (DynamicBlockers.Count == 0)
                return true;

            if (ignoreOwner == null)
                return false;

            // Walkable if the only blocker is the ignored owner
            return DynamicBlockers.Count == 1 && DynamicBlockers.Contains(ignoreOwner);
        }

        public bool IsDamagingOrNear => IsDamaging || IsNearDamaging;
    }

    private const float GIZMOS_WAYPOINT_RADIUS = 0.1f;
    private const float COLLINEARITY_THRESHOLD = 0.001f;

    [Header("Tilemaps")]
    [SerializeField] private Tilemap[] Tilemaps;

    [Header("Dynamic Obstacle Cache")]
    [SerializeField] private float ObstacleCacheLifetime = 0.5f;
    [SerializeField] private int DistanceFromDamagings = 1;

    [Header("Grid Settings")]
    [SerializeField] private int GridSubdivision = 2;
    [SerializeField] private WaypointAlignment WaypointAlignment = WaypointAlignment.Center;

    [Header("Debug")]
    [SerializeField] private bool ShowPathGizmos = true;

    private PathfinderNode[,] _grid;
    private Vector3Int _gridOrigin;
    private Vector2Int _gridSize;
    private Vector2 _cellSize;
    private float _lastObstacleRefreshTime;

    private Vector2[] _lastCalculatedPath;
    private Vector2 _lastPathStart;
    private Vector2 _lastUnreachableTarget;
    private bool _lastTargetWasAdjusted;

    private Vector2 _subCellSize;

    protected override void Awake()
    {
        if (!base.CanAwake()) return;
    }

    private void Start()
    {
        BuildGrid();
    }

    private void BuildGrid()
    {
        if (Tilemaps == null || Tilemaps.Length == 0)
        {
            Debug.LogError("PathfinderManager: No tilemaps assigned!");
            return;
        }

        // 1. Compress bounds and calculate union of all tilemaps
        foreach (Tilemap tilemap in Tilemaps)
        {
            tilemap.CompressBounds();
        }

        BoundsInt unionBounds = Tilemaps[0].cellBounds;
        for (int i = 1; i < Tilemaps.Length; i++)
        {
            unionBounds.xMin = Mathf.Min(unionBounds.xMin, Tilemaps[i].cellBounds.xMin);
            unionBounds.yMin = Mathf.Min(unionBounds.yMin, Tilemaps[i].cellBounds.yMin);
            unionBounds.xMax = Mathf.Max(unionBounds.xMax, Tilemaps[i].cellBounds.xMax);
            unionBounds.yMax = Mathf.Max(unionBounds.yMax, Tilemaps[i].cellBounds.yMax);
        }

        _gridOrigin = unionBounds.min;
        _cellSize = Tilemaps[0].cellSize;
        _subCellSize = _cellSize / GridSubdivision;
        _gridSize = new Vector2Int(unionBounds.size.x * GridSubdivision, unionBounds.size.y * GridSubdivision);

        // 2. Create grid array
        _grid = new PathfinderNode[_gridSize.x, _gridSize.y];

        // 3. Populate grid cells (subdivided)
        for (int x = 0; x < _gridSize.x; x++)
        {
            for (int y = 0; y < _gridSize.y; y++)
            {
                // Calculate world position for this sub-cell
                Vector2 worldPosition = GetSubCellWorldPosition(x, y);

                bool isTilemapWalkable = !HasTileWithColliderAtPosition(worldPosition);

                _grid[x, y] = new PathfinderNode
                {
                    GridPosition = new Vector2Int(x, y),
                    WorldPosition = worldPosition,
                    IsTilemapWalkable = isTilemapWalkable
                };
            }
        }

        // 4. Initial scan of GameObjects with colliders
        RefreshDynamicObstacles();
    }

    private Vector2 GetSubCellWorldPosition(int gridX, int gridY)
    {
        float baseX = (_gridOrigin.x * _cellSize.x) + (gridX * _subCellSize.x);
        float baseY = (_gridOrigin.y * _cellSize.y) + (gridY * _subCellSize.y);

        return WaypointAlignment switch
        {
            WaypointAlignment.Left => new Vector2(baseX, baseY + (_subCellSize.y * 0.5f)),
            WaypointAlignment.Right => new Vector2(baseX + _subCellSize.x, baseY + (_subCellSize.y * 0.5f)),
            WaypointAlignment.Top => new Vector2(baseX + (_subCellSize.x * 0.5f), baseY + _subCellSize.y),
            WaypointAlignment.Bottom => new Vector2(baseX + (_subCellSize.x * 0.5f), baseY),
            _ => new Vector2(baseX + (_subCellSize.x * 0.5f), baseY + (_subCellSize.y * 0.5f))
        };
    }

    private bool HasTileWithColliderAtPosition(Vector2 worldPosition)
    {
        float cellRadius = Mathf.Min(_subCellSize.x, _subCellSize.y) * 0.4f;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(worldPosition, cellRadius);
        foreach (Collider2D collider in colliders)
        {
            if (collider.isTrigger)
                continue;

            if (IsTilemapCollider(collider))
                return true;
        }

        return false;
    }

    private void RefreshDynamicObstacles()
    {
        // Reset dynamic blockers and damaging status
        for (int x = 0; x < _gridSize.x; x++)
        {
            for (int y = 0; y < _gridSize.y; y++)
            {
                _grid[x, y].DynamicBlockers.Clear();
                _grid[x, y].IsDamaging = false;
                _grid[x, y].IsNearDamaging = false;
            }
        }

        Collider2D[] allColliders = FindObjectsByType<Collider2D>(FindObjectsSortMode.None);

        foreach (Collider2D collider in allColliders)
        {
            if (!collider.gameObject.activeInHierarchy || IsTilemapCollider(collider))
                continue;

            Transform owner = collider.transform;

            // Mark non-trigger colliders as unwalkable obstacles
            if (!collider.isTrigger)
            {
                MarkNodesInBoundsAsBlocked(collider.bounds, owner);
            }

            // Mark colliders with Damaging component as damaging
            if (collider.TryGetComponent<Damaging>(out _))
            {
                MarkNodesInBoundsAsDamaging(collider.bounds);
            }
        }

        // Expand damaging zones to include nearby nodes
        ExpandDamagingRadius();

        _lastObstacleRefreshTime = Time.time;
    }

    private void ExpandDamagingRadius()
    {
        if (DistanceFromDamagings <= 0)
            return;

        // Collect all directly damaging nodes first to avoid expanding from already-expanded nodes
        List<PathfinderNode> damagingNodes = new List<PathfinderNode>();
        for (int x = 0; x < _gridSize.x; x++)
        {
            for (int y = 0; y < _gridSize.y; y++)
            {
                if (_grid[x, y].IsDamaging)
                {
                    damagingNodes.Add(_grid[x, y]);
                }
            }
        }

        // Mark nodes within radius as IsNearDamaging
        foreach (PathfinderNode damagingNode in damagingNodes)
        {
            for (int dx = -DistanceFromDamagings; dx <= DistanceFromDamagings; dx++)
            {
                for (int dy = -DistanceFromDamagings; dy <= DistanceFromDamagings; dy++)
                {
                    if (dx == 0 && dy == 0)
                        continue;

                    int checkX = damagingNode.GridPosition.x + dx;
                    int checkY = damagingNode.GridPosition.y + dy;

                    if (checkX >= 0 && checkX < _gridSize.x && checkY >= 0 && checkY < _gridSize.y)
                    {
                        PathfinderNode node = _grid[checkX, checkY];
                        if (!node.IsDamaging)
                        {
                            node.IsNearDamaging = true;
                        }
                    }
                }
            }
        }
    }

    private void GetNodeBoundsIndices(Bounds bounds, out int startX, out int startY, out int endX, out int endY)
    {
        // Convert bounds to subdivided grid indices
        startX = Mathf.Max(0, Mathf.FloorToInt((bounds.min.x - (_gridOrigin.x * _cellSize.x)) / _subCellSize.x));
        startY = Mathf.Max(0, Mathf.FloorToInt((bounds.min.y - (_gridOrigin.y * _cellSize.y)) / _subCellSize.y));
        endX = Mathf.Min(_gridSize.x - 1, Mathf.FloorToInt((bounds.max.x - (_gridOrigin.x * _cellSize.x)) / _subCellSize.x));
        endY = Mathf.Min(_gridSize.y - 1, Mathf.FloorToInt((bounds.max.y - (_gridOrigin.y * _cellSize.y)) / _subCellSize.y));
    }

    private void MarkNodesInBoundsAsBlocked(Bounds bounds, Transform owner)
    {
        GetNodeBoundsIndices(bounds, out int startX, out int startY, out int endX, out int endY);

        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                PathfinderNode node = _grid[x, y];
                if (node.IsTilemapWalkable)
                {
                    node.DynamicBlockers.Add(owner);
                }
            }
        }
    }

    private void MarkNodesInBoundsAsDamaging(Bounds bounds)
    {
        GetNodeBoundsIndices(bounds, out int startX, out int startY, out int endX, out int endY);

        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                _grid[x, y].IsDamaging = true;
            }
        }
    }

    private bool IsTilemapCollider(Collider2D collider)
    {
        // Direct TilemapCollider2D
        if (collider is TilemapCollider2D)
            return true;

        // CompositeCollider2D that belongs to a Tilemap (used when TilemapCollider2D has "Used by Composite" enabled)
        if (collider is CompositeCollider2D && collider.GetComponent<Tilemap>() != null)
            return true;

        return false;
    }

    /// <summary>
    /// Gets the route from currentPosition to a specific target using A* pathfinding.
    /// Automatically refreshes dynamic obstacles if cache is stale.
    /// </summary>
    /// <param name="avoidDamagings">When true, avoids nodes with Damaging components.</param>
    /// <param name="ignoreOwner">Optional transform to ignore as an obstacle (e.g., the requesting entity's own transform).</param>
    public Vector2[] GetRouteToTarget(Vector2 currentPosition, Vector2 targetPosition, bool avoidDamagings = true, Transform ignoreOwner = null)
    {
        if (!EnsureGridReady())
            return System.Array.Empty<Vector2>();

        Vector2[] path = FindPath(currentPosition, targetPosition, avoidDamagings, ignoreOwner, out bool targetWasAdjusted);
        CachePathForGizmos(currentPosition, path, targetWasAdjusted, targetPosition);
        return path;
    }

    /// <summary>
    /// Gets the route to the target nearest by straight-line distance (ignoring obstacles).
    /// Fast approximation - for accurate shortest path considering obstacles, use GetRouteToTargetWithShortestPath.
    /// </summary>
    /// <param name="avoidDamagings">When true, avoids nodes with Damaging components.</param>
    /// <param name="ignoreOwner">Optional transform to ignore as an obstacle (e.g., the requesting entity's own transform).</param>
    public Vector2[] GetRouteToNearestTarget(Vector2 currentPosition, List<Vector2> targetPositions, bool avoidDamagings = true, Transform ignoreOwner = null)
    {
        if (targetPositions == null || targetPositions.Count == 0)
            return System.Array.Empty<Vector2>();

        Vector2 nearest = targetPositions[0];
        float nearestDistanceSqr = (currentPosition - nearest).sqrMagnitude;

        for (int i = 1; i < targetPositions.Count; i++)
        {
            float distanceSqr = (currentPosition - targetPositions[i]).sqrMagnitude;
            if (distanceSqr < nearestDistanceSqr)
            {
                nearestDistanceSqr = distanceSqr;
                nearest = targetPositions[i];
            }
        }

        return GetRouteToTarget(currentPosition, nearest, avoidDamagings, ignoreOwner);
    }

    /// <summary>
    /// Gets the route to the target with the shortest pathfinding route (not straight-line distance).
    /// </summary>
    /// <param name="avoidDamagings">When true, avoids nodes with Damaging components.</param>
    /// <param name="ignoreOwner">Optional transform to ignore as an obstacle (e.g., the requesting entity's own transform).</param>
    public Vector2[] GetRouteToTargetWithShortestPath(Vector2 currentPosition, List<Vector2> targetPositions, bool avoidDamagings = true, Transform ignoreOwner = null)
    {
        if (targetPositions == null || targetPositions.Count == 0)
            return System.Array.Empty<Vector2>();

        if (!EnsureGridReady())
            return System.Array.Empty<Vector2>();

        Vector2[] shortestPath = null;
        int shortestPathLength = int.MaxValue;
        bool shortestTargetWasAdjusted = false;
        Vector2 shortestUnreachableTarget = default;

        foreach (Vector2 target in targetPositions)
        {
            Vector2[] path = FindPath(currentPosition, target, avoidDamagings, ignoreOwner, out bool targetWasAdjusted);

            if (path.Length > 0 && path.Length < shortestPathLength)
            {
                shortestPath = path;
                shortestPathLength = path.Length;
                shortestTargetWasAdjusted = targetWasAdjusted;
                shortestUnreachableTarget = target;
            }
        }

        Vector2[] result = shortestPath ?? System.Array.Empty<Vector2>();
        CachePathForGizmos(currentPosition, result, shortestTargetWasAdjusted, shortestUnreachableTarget);
        return result;
    }

    private bool EnsureGridReady()
    {
        if (_grid == null)
        {
            Debug.LogError("PathfinderManager: Grid not initialized!");
            return false;
        }

        if (Time.time - _lastObstacleRefreshTime > ObstacleCacheLifetime)
        {
            RefreshDynamicObstacles();
        }

        return true;
    }

    private void CachePathForGizmos(Vector2 startPosition, Vector2[] path, bool targetWasAdjusted = false, Vector2 unreachableTarget = default)
    {
        _lastPathStart = startPosition;
        _lastCalculatedPath = path;
        _lastTargetWasAdjusted = targetWasAdjusted;
        _lastUnreachableTarget = unreachableTarget;
    }

    private Vector2[] FindPath(Vector2 startWorldPos, Vector2 targetWorldPos, bool avoidDamagings, Transform ignoreOwner, out bool targetWasAdjusted)
    {
        targetWasAdjusted = false;

        // Convert world positions to grid positions
        PathfinderNode startNode = GetNodeFromWorldPosition(startWorldPos);
        PathfinderNode targetNode = GetNodeFromWorldPosition(targetWorldPos);

        if (startNode == null || targetNode == null)
        {
            return System.Array.Empty<Vector2>();
        }

        if (!IsNodeTraversable(targetNode, avoidDamagings, ignoreOwner))
        {
            // Target is unwalkable or damaging, try to find nearest valid node
            targetNode = GetNearestTraversableNode(targetNode, avoidDamagings, ignoreOwner);
            if (targetNode == null)
            {
                return System.Array.Empty<Vector2>();
            }
            targetWasAdjusted = true;
        }

        // Reset pathfinding data for all nodes
        for (int x = 0; x < _gridSize.x; x++)
        {
            for (int y = 0; y < _gridSize.y; y++)
            {
                _grid[x, y].ResetPathfindingData();
            }
        }

        List<PathfinderNode> openSet = new List<PathfinderNode>();
        HashSet<PathfinderNode> closedSet = new HashSet<PathfinderNode>();

        startNode.GCost = 0;
        startNode.HCost = GetDistance(startNode, targetNode);
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            // Find node with lowest FCost
            PathfinderNode currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].FCost < currentNode.FCost ||
                    (openSet[i].FCost == currentNode.FCost && openSet[i].HCost < currentNode.HCost))
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            // Path found
            if (currentNode == targetNode)
            {
                Vector2[] path = RetracePath(startNode, targetNode);

                // Use exact target position if target wasn't adjusted
                if (path.Length > 0 && !targetWasAdjusted)
                {
                    path[path.Length - 1] = targetWorldPos;
                }

                return SimplifyPath(path);
            }

            // Check neighbors
            foreach (PathfinderNode neighbor in GetNeighbors(currentNode))
            {
                if (!IsNodeTraversable(neighbor, avoidDamagings, ignoreOwner) || closedSet.Contains(neighbor))
                {
                    continue;
                }

                float newGCost = currentNode.GCost + GetDistance(currentNode, neighbor);

                if (newGCost < neighbor.GCost)
                {
                    neighbor.GCost = newGCost;
                    neighbor.HCost = GetDistance(neighbor, targetNode);
                    neighbor.Parent = currentNode;

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        // No path found
        return System.Array.Empty<Vector2>();
    }

    private PathfinderNode GetNodeFromWorldPosition(Vector2 worldPos)
    {
        // Convert world position to subdivided grid coordinates
        int x = Mathf.FloorToInt((worldPos.x - (_gridOrigin.x * _cellSize.x)) / _subCellSize.x);
        int y = Mathf.FloorToInt((worldPos.y - (_gridOrigin.y * _cellSize.y)) / _subCellSize.y);

        if (x >= 0 && x < _gridSize.x && y >= 0 && y < _gridSize.y)
        {
            return _grid[x, y];
        }

        return null;
    }

    private bool IsNodeTraversable(PathfinderNode node, bool avoidDamagings, Transform ignoreOwner = null)
    {
        bool isWalkable = ignoreOwner != null
            ? node.IsWalkableIgnoring(ignoreOwner)
            : node.IsWalkable;

        if (!isWalkable)
            return false;

        if (avoidDamagings && node.IsDamagingOrNear)
            return false;

        return true;
    }

    private PathfinderNode GetNearestTraversableNode(PathfinderNode invalidNode, bool avoidDamagings, Transform ignoreOwner = null)
    {
        // Simple spiral search for nearest traversable node
        for (int radius = 1; radius < Mathf.Max(_gridSize.x, _gridSize.y); radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    int checkX = invalidNode.GridPosition.x + x;
                    int checkY = invalidNode.GridPosition.y + y;

                    if (checkX >= 0 && checkX < _gridSize.x && checkY >= 0 && checkY < _gridSize.y)
                    {
                        PathfinderNode node = _grid[checkX, checkY];
                        if (IsNodeTraversable(node, avoidDamagings, ignoreOwner))
                        {
                            return node;
                        }
                    }
                }
            }
        }

        return null;
    }

    private List<PathfinderNode> GetNeighbors(PathfinderNode node)
    {
        List<PathfinderNode> neighbors = new List<PathfinderNode>(8);

        // 8-directional movement
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue; // Skip self

                int checkX = node.GridPosition.x + x;
                int checkY = node.GridPosition.y + y;

                if (checkX >= 0 && checkX < _gridSize.x && checkY >= 0 && checkY < _gridSize.y)
                {
                    neighbors.Add(_grid[checkX, checkY]);
                }
            }
        }

        return neighbors;
    }

    private float GetDistance(PathfinderNode nodeA, PathfinderNode nodeB)
    {
        // Octile distance for 8-directional movement (diagonal costs √2)
        int distX = Mathf.Abs(nodeA.GridPosition.x - nodeB.GridPosition.x);
        int distY = Mathf.Abs(nodeA.GridPosition.y - nodeB.GridPosition.y);

        const float DIAGONAL_COST = 1.41421356f;
        int diagonal = Mathf.Min(distX, distY);
        int straight = Mathf.Abs(distX - distY);

        return (diagonal * DIAGONAL_COST) + straight;
    }

    private Vector2[] RetracePath(PathfinderNode startNode, PathfinderNode endNode)
    {
        List<Vector2> path = new List<Vector2>();
        PathfinderNode currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode.WorldPosition);
            currentNode = currentNode.Parent;
        }

        path.Reverse();
        return path.ToArray();
    }

    private Vector2[] SimplifyPath(Vector2[] path)
    {
        if (path.Length <= 2)
            return path;

        List<Vector2> simplified = new List<Vector2> { path[0] };

        for (int i = 1; i < path.Length - 1; i++)
        {
            Vector2 prev = simplified[simplified.Count - 1];
            Vector2 current = path[i];
            Vector2 next = path[i + 1];

            // Check if current point is collinear with prev and next using cross product
            // Cross product of (current - prev) and (next - prev)
            float cross = (current.x - prev.x) * (next.y - prev.y) - (current.y - prev.y) * (next.x - prev.x);

            // If not collinear, keep this point
            if (Mathf.Abs(cross) > COLLINEARITY_THRESHOLD)
            {
                simplified.Add(current);
            }
        }

        // Always add the last point
        simplified.Add(path[path.Length - 1]);

        return simplified.ToArray();
    }

    private void OnDrawGizmos()
    {
        DrawPathGizmos();
    }

    private void DrawPathGizmos()
    {
        if (!ShowPathGizmos || _lastCalculatedPath == null || _lastCalculatedPath.Length == 0)
            return;

        Gizmos.color = Color.cyan;

        Gizmos.DrawLine(_lastPathStart, _lastCalculatedPath[0]);

        for (int i = 0; i < _lastCalculatedPath.Length - 1; i++)
        {
            Gizmos.DrawLine(_lastCalculatedPath[i], _lastCalculatedPath[i + 1]);
        }

        foreach (Vector2 waypoint in _lastCalculatedPath)
        {
            Gizmos.DrawSphere(waypoint, GIZMOS_WAYPOINT_RADIUS);
        }

        // Draw red line to unreachable target if it was adjusted
        if (_lastTargetWasAdjusted)
        {
            Gizmos.color = Color.red;
            Vector2 lastReachable = _lastCalculatedPath[_lastCalculatedPath.Length - 1];
            Gizmos.DrawLine(lastReachable, _lastUnreachableTarget);
            Gizmos.DrawWireSphere(_lastUnreachableTarget, GIZMOS_WAYPOINT_RADIUS * 1.5f);
        }
    }
}
