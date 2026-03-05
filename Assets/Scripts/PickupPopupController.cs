using RobsonRocha.UnityCommon;
using System;
using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-85)]
[RequireComponent(typeof(PixelText))]
public class PickupPopupController : MonoBehaviour
{
    private PixelText _promptText;
    [SerializeField] private AnimationCurve MovementEasing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Coroutine _animationRoutine;

    private void Awake()
    {
        _promptText = GetComponent<PixelText>();
    }

    public void Show(
        Vector3 worldPosition,
        string text, 
        Color color = default, 
        Direction direction = Direction.Up, 
        float speed = 0.5f, 
        float duration = 2f,
        Action onFinish = null)
    {
        if (_animationRoutine != null)
        {
            StopCoroutine(_animationRoutine);
        }

        transform.position = worldPosition;
        gameObject.SetActive(true);
        
        _promptText.Render(text, textColor: color != default ? color : null, alpha: 0f);
        
        _animationRoutine = StartCoroutine(AnimationRoutine(direction.ToVector2(), speed, duration, onFinish));
    }

    private IEnumerator AnimationRoutine(Vector3 moveDirection, float speed, float duration, Action onFinish)
    {
        Vector3 startPosition = transform.position;
        float totalDistance = speed * duration;
        float fadeInDuration = duration * 0.15f;
        float fadeOutStartTime = duration * 0.75f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float easedProgress = MovementEasing.Evaluate(t);
            transform.position = startPosition + easedProgress * totalDistance * moveDirection;

            if (elapsed < fadeInDuration)
            {
                _promptText.SetAlpha(Mathf.Lerp(0f, 1f, elapsed / fadeInDuration));
            }
            else if (elapsed >= fadeOutStartTime)
            {
                float fadeProgress = (elapsed - fadeOutStartTime) / (duration - fadeOutStartTime);
                _promptText.SetAlpha(Mathf.Lerp(1f, 0f, fadeProgress));
            }
            else
            {
                _promptText.SetAlpha(1f);
            }

            yield return null;
        }

        onFinish?.Invoke();
        Destroy(gameObject);
    }
}
