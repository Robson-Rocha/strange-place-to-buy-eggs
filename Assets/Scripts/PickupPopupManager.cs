using RobsonRocha.UnityCommon;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-90)]
public class PickupPopupManager : SingletonMonoBehaviour<PickupPopupManager>
{
    [SerializeField] private PickupPopupController PickupPopupPrefab;
    [SerializeField] private Transform CanvasTransform;

    private const float POSITION_TOLERANCE = 0.5f;
    private readonly Dictionary<Vector3, float> _nextAvailableTimeByPosition = new();

    protected override void Awake()
    {
        base.CanAwake();
    }

    public void ShowPopup(
        string text, Vector3 worldPosition,
        Color color = default, Direction direction = Direction.Up,
        float speed = 0.5f, float duration = 2f, SoundEffect soundEffect = null, bool immediate = false)
    {
        if (PickupPopupPrefab == null || CanvasTransform == null)
        {
            Debug.LogWarning("PickupPopupManager: Missing prefab or canvas transform.");
            return;
        }

        float delay = 0f;

        if (!immediate)
        {
            float staggerInterval = Mathf.Max(0.1f, 0.3f / speed);
            delay = CalculateDelay(worldPosition, staggerInterval);
        }

        PickupPopupController popup = Instantiate(PickupPopupPrefab, CanvasTransform);

        if (delay > 0f)
            StartCoroutine(DelayedShow(popup, worldPosition, text, color, direction, speed, duration, delay, soundEffect));
        else
        {
            popup.Show(worldPosition, text, color, direction, speed, duration);
            if (soundEffect != null) SoundManager.Instance.PlaySfx(soundEffect);
        }
    }

    private float CalculateDelay(Vector3 worldPosition, float staggerInterval)
    {
        foreach (var key in _nextAvailableTimeByPosition.Keys)
        {
            if (Vector3.Distance(key, worldPosition) < POSITION_TOLERANCE)
            {
                float delay = Mathf.Max(0f, _nextAvailableTimeByPosition[key] - Time.time);
                _nextAvailableTimeByPosition[key] = Time.time + delay + staggerInterval;
                return delay;
            }
        }

        _nextAvailableTimeByPosition[worldPosition] = Time.time + staggerInterval;
        return 0f;
    }

    private IEnumerator DelayedShow(PickupPopupController popup, Vector3 worldPosition, string text, Color color, Direction direction, float speed, float duration, float delay, SoundEffect soundEffect)
    {
        yield return new WaitForSeconds(delay);
        popup.Show(worldPosition, text, color, direction, speed, duration);
        if (soundEffect != null) SoundManager.Instance.PlaySfx(soundEffect);
    }
}
