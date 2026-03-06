using RobsonRocha.UnityCommon;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Moveable))]
[DefaultExecutionOrder(10)]
public class RandomFacingDirectionAiBehaviour : MonoBehaviour, IAiBehaviour
{
    [Header("Behaviour Settings")]
    [SerializeField][Range(-100, 100)] private int BehaviourPriority = -100;
    [SerializeField] private bool IsDisabled = false;

    [Header("Facing Direction Settings")]
    [SerializeField] private float ChangeDirectionIntervalMin = 2f;
    [SerializeField] private float ChangeDirectionIntervalMax = 5f;
    [SerializeField] private Direction AllowedFacingDirections = Direction.Down;

    public int Priority => BehaviourPriority;

    public bool CanAct { get; private set; }

    public bool IsBlocking => false;

    public void Sense() =>
        CanAct = !IsDisabled && AllowedFacingDirections != Direction.None; 

    private float _directionChangeTimer;
    
    private Moveable _moveable;

    void Awake()
    {
        this.TryInitComponent(ref _moveable);
    }

    void Update()
    {
        if (_directionChangeTimer.DecrementTimer().IsAboveNearZero())
            return;

        ChangeFacingDirection();
    }

    void OnEnable()
    {
        ResetTimer();
    }

    private void ChangeFacingDirection()
    {
        ResetTimer();

        List<Direction> possibleDirections = new();

        if (AllowedFacingDirections.IsUp()) possibleDirections.Add(Direction.Up);
        if (AllowedFacingDirections.IsDown()) possibleDirections.Add(Direction.Down);
        if (AllowedFacingDirections.IsLeft()) possibleDirections.Add(Direction.Left);
        if (AllowedFacingDirections.IsRight()) possibleDirections.Add(Direction.Right);

        if (possibleDirections.Count == 0)
            return;

        Direction selectedDirection = possibleDirections[Random.Range(0, possibleDirections.Count)];
        _moveable.FaceDirection(selectedDirection.ToVector2());
    }

    private void ResetTimer() =>
        _directionChangeTimer = Random.Range(ChangeDirectionIntervalMin, ChangeDirectionIntervalMax);
}
