using System.Collections.Generic;
using RobsonRocha.UnityCommon;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Singleton service for simple pathfinding operations.
/// </summary>
[DefaultExecutionOrder(-10)]
public class SimplePathfinderManager : SingletonMonoBehaviour<SimplePathfinderManager>, IPathfinder
{
    protected override void Awake()
    {
        if (!base.CanAwake()) return;
    }

    /// <summary>
    /// Gets the route from currentPosition to the nearest target in the list.
    /// </summary>
    public Vector2[] GetRouteToNearestTarget(
        Vector2 currentPosition, List<Vector2> targetPositions, bool avoidDamagings = true, Transform ignoreOwner = null)
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
    /// Gets the route from currentPosition to a specific target.
    /// </summary>
    public Vector2[] GetRouteToTarget(
        Vector2 currentPosition, Vector2 targetPosition, bool avoidDamagings = true, Transform ignoreOwner = null) =>
            new Vector2[] { targetPosition };

    public Vector2[] GetRouteToTargetWithShortestPath(
        Vector2 currentPosition, List<Vector2> targetPositions, bool avoidDamagings = true, Transform ignoreOwner = null) =>
            GetRouteToNearestTarget(currentPosition, targetPositions, avoidDamagings, ignoreOwner);
}
