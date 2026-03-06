using System.Collections.Generic;
using RobsonRocha.UnityCommon;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Singleton service for pathfinding operations.
/// Currently provides mock implementation returning direct paths.
/// Future: Will use A* with tilemap-based obstacle detection.
/// </summary>
public class PathfinderManager : SingletonMonoBehaviour<PathfinderManager>
{
    [Header("Tilemaps")]
    [SerializeField] private Tilemap[] Tilemaps;

    /// <summary>
    /// Gets the route from currentPosition to the nearest target in the list.
    /// Mock: Returns single-element array with nearest position.
    /// Future A*: Returns full path with intermediate nodes.
    /// </summary>
    public Vector2[] GetRouteToNearestTarget(Vector2 currentPosition, List<Vector2> targetPositions)
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

        return GetRouteToTarget(currentPosition, nearest);
    }

    /// <summary>
    /// Gets the route from currentPosition to a specific target.
    /// Mock: Returns single-element array with targetPosition.
    /// Future A*: Returns full path with intermediate nodes.
    /// </summary>
    public Vector2[] GetRouteToTarget(Vector2 currentPosition, Vector2 targetPosition)
    {
        // Mock implementation: direct path (single node)
        // Future: A* pathfinding using tilemaps
        return new Vector2[] { targetPosition };
    }
}
