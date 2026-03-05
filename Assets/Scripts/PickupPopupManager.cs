using RobsonRocha.UnityCommon;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-90)]
public class PickupPopupManager : SingletonMonoBehaviour<PickupPopupManager>
{
    [SerializeField] private PickupPopupController PickupPopupPrefab;

    private const float POSITION_TOLERANCE = 0.5f;
    private const float POSITION_TOLERANCE_SQR = POSITION_TOLERANCE * POSITION_TOLERANCE;
    private readonly Dictionary<Vector3, int> _activePopupCountByPosition = new();
    
    // Cache list to avoid allocating during iteration
    private readonly List<Vector3> _keysCache = new();

    protected override void Awake()
    {
        base.CanAwake();
    }

    public void ShowPopup(
        string text, Vector3 worldPosition,
        Color color = default, Direction direction = Direction.Up,
        float speed = 0.5f, float duration = 2f, SoundEffect soundEffect = null)
    {
        if (PickupPopupPrefab == null)
        {
            Debug.LogWarning("PickupPopupManager: Missing prefab.");
            return;
        }

        speed = CalculateSpeed(worldPosition, speed);
        PickupPopupController popup = Instantiate(PickupPopupPrefab);
        popup.Show(worldPosition, text, color, direction, speed, duration, () => HandlePopupComplete(worldPosition));
        if (soundEffect != null) SoundManager.Instance.PlaySfx(soundEffect);
    }

    private float CalculateSpeed(Vector3 worldPosition, float speed)
    {
        // Use sqrMagnitude to avoid sqrt allocation
        foreach (var kvp in _activePopupCountByPosition)
        {
            if ((kvp.Key - worldPosition).sqrMagnitude < POSITION_TOLERANCE_SQR)
            {
                int newCount = kvp.Value + 1;
                _activePopupCountByPosition[kvp.Key] = newCount;
                return newCount * speed;
            }
        }

        _activePopupCountByPosition.Add(worldPosition, 1);
        return speed;
    }

    private void HandlePopupComplete(Vector3 worldPosition)
    {
        Vector3 keyToRemove = default;
        bool found = false;
        
        foreach (var kvp in _activePopupCountByPosition)
        {
            if ((kvp.Key - worldPosition).sqrMagnitude < POSITION_TOLERANCE_SQR)
            {
                if (kvp.Value <= 1)
                {
                    keyToRemove = kvp.Key;
                    found = true;
                }
                else
                {
                    _activePopupCountByPosition[kvp.Key] = kvp.Value - 1;
                }
                break;
            }
        }
        
        if (found)
        {
            _activePopupCountByPosition.Remove(keyToRemove);
        }
    }
}
