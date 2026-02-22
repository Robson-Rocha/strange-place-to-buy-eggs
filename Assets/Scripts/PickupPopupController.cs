using RobsonRocha.UnityCommon;
using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
[DefaultExecutionOrder(-85)]
public class PickupPopupController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI PromptText;
    [SerializeField] private CanvasGroup CanvasGroup;
    [SerializeField] private AnimationCurve MovementEasing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Coroutine _animationRoutine;

    private void Awake()
    {
        CanvasGroup = GetComponent<CanvasGroup>();
    }

    public void Show(
        Vector3 worldPosition,
        string text, 
        Color color = default, 
        Direction direction = Direction.Up, 
        float speed = 0.5f, 
        float duration = 2f)
    {
        PromptText.text = text;

        if (color != default)
        {
            PromptText.color = color;
        }

        if (_animationRoutine != null)
        {
            StopCoroutine(_animationRoutine);
        }

        transform.position = worldPosition;
        CanvasGroup.alpha = 0f;
        gameObject.SetActive(true);
        _animationRoutine = StartCoroutine(AnimationRoutine(direction.ToVector2(), speed, duration));
    }

    private IEnumerator AnimationRoutine(Vector3 moveDirection, float speed, float duration)
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
            transform.position = startPosition + moveDirection * totalDistance * easedProgress;

            if (elapsed < fadeInDuration)
            {
                CanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            }
            else if (elapsed >= fadeOutStartTime)
            {
                float fadeProgress = (elapsed - fadeOutStartTime) / (duration - fadeOutStartTime);
                CanvasGroup.alpha = Mathf.Lerp(1f, 0f, fadeProgress);
            }
            else
            {
                CanvasGroup.alpha = 1f;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
