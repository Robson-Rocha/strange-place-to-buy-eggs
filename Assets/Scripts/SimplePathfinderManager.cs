using System.Collections.Generic;
using RobsonRocha.UnityCommon;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Singleton service for simple pathfinding operations.
/// Uses direct routes and stops before obstacles/damaging colliders.
/// </summary>
[DefaultExecutionOrder(-10)]
public class SimplePathfinderManager : SingletonMonoBehaviour<SimplePathfinderManager>, IPathfinder
{
    private const float MIN_DIRECTION_SQR_MAGNITUDE = 0.0001f;
    private const float MIN_ROUTE_DISTANCE = 0.01f;

    [Header("Direct Path Safety")]
    [SerializeField] private float StopBeforeHitDistance = 0.05f;

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
    /// If a blocking obstacle/damaging collider is found in the direct path,
    /// returns a waypoint right before the hit.
    /// </summary>
    public Vector2[] GetRouteToTarget(
        Vector2 currentPosition, Vector2 targetPosition, bool avoidDamagings = true, Transform ignoreOwner = null)
    {
        if (TryGetAdjustedTargetBeforeBlockingHit(
            currentPosition,
            targetPosition,
            avoidDamagings,
            ignoreOwner,
            out Vector2 adjustedTarget))
        {
            if ((adjustedTarget - currentPosition).sqrMagnitude < (MIN_ROUTE_DISTANCE * MIN_ROUTE_DISTANCE))
                return System.Array.Empty<Vector2>();

            return new[] { adjustedTarget };
        }

        return new[] { targetPosition };
    }

    public Vector2[] GetRouteToTargetWithShortestPath(
        Vector2 currentPosition, List<Vector2> targetPositions, bool avoidDamagings = true, Transform ignoreOwner = null) =>
            GetRouteToNearestTarget(currentPosition, targetPositions, avoidDamagings, ignoreOwner);

    private bool TryGetAdjustedTargetBeforeBlockingHit(
        Vector2 start,
        Vector2 target,
        bool avoidDamagings,
        Transform ignoreOwner,
        out Vector2 adjustedTarget)
    {
        adjustedTarget = target;

        Vector2 direction = target - start;
        if (direction.sqrMagnitude < MIN_DIRECTION_SQR_MAGNITUDE)
            return false;

        RaycastHit2D[] hits = Physics2D.LinecastAll(start, target);

        bool foundBlocking = false;
        RaycastHit2D nearestBlockingHit = default;
        float nearestHitDistanceSqr = float.MaxValue;

        foreach (RaycastHit2D hit in hits)
        {
            Collider2D collider = hit.collider;
            if (collider == null || !collider.gameObject.activeInHierarchy)
                continue;

            if (ShouldIgnoreCollider(collider, ignoreOwner))
                continue;

            bool isObstacle = !collider.isTrigger || IsTilemapCollider(collider);
            bool isDamaging = avoidDamagings && HasDamagingComponent(collider);

            if (!isObstacle && !isDamaging)
                continue;

            float hitDistanceSqr = (hit.point - start).sqrMagnitude;
            if (hitDistanceSqr < nearestHitDistanceSqr)
            {
                nearestHitDistanceSqr = hitDistanceSqr;
                nearestBlockingHit = hit;
                foundBlocking = true;
            }
        }

        if (!foundBlocking)
            return false;

        Vector2 normalizedDirection = direction.normalized;
        adjustedTarget = nearestBlockingHit.point - (normalizedDirection * StopBeforeHitDistance);
        return true;
    }

    private bool ShouldIgnoreCollider(Collider2D collider, Transform ignoreOwner)
    {
        if (ignoreOwner == null)
            return false;

        Transform hitTransform = collider.transform;

        if (hitTransform == ignoreOwner)
            return true;

        if (hitTransform.IsChildOf(ignoreOwner))
            return true;

        if (ignoreOwner.IsChildOf(hitTransform))
            return true;

        return false;
    }

    private bool HasDamagingComponent(Collider2D collider)
    {
        if (collider.TryGetComponent<Damaging>(out _))
            return true;

        return collider.GetComponentInParent<Damaging>() != null;
    }

    private bool IsTilemapCollider(Collider2D collider)
    {
        if (collider is TilemapCollider2D)
            return true;

        if (collider is CompositeCollider2D && collider.GetComponent<Tilemap>() != null)
            return true;

        return false;
    }
}
