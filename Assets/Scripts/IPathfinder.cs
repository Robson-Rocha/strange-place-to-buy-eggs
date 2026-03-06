using System.Collections.Generic;
using UnityEngine;

public interface IPathfinder
{
    Vector2[] GetRouteToNearestTarget(Vector2 currentPosition, List<Vector2> targetPositions, bool avoidDamagings = true, Transform ignoreOwner = null);
    Vector2[] GetRouteToTarget(Vector2 currentPosition, Vector2 targetPosition, bool avoidDamagings = true, Transform ignoreOwner = null);
    Vector2[] GetRouteToTargetWithShortestPath(Vector2 currentPosition, List<Vector2> targetPositions, bool avoidDamagings = true, Transform ignoreOwner = null);
}