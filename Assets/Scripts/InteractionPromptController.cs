using RobsonRocha.UnityCommon;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
[DefaultExecutionOrder(-85)]
public class InteractionPromptController : MonoBehaviour
{
    [SerializeField] private Image GlyphImage;
    [SerializeField] private TextMeshProUGUI PromptText;
    [SerializeField] private CanvasGroup CanvasGroup;
    [SerializeField] private float FadeDuration = 0.05f;

    private Coroutine _fadeRoutine;

    private void Awake()
    {
        CanvasGroup = GetComponent<CanvasGroup>();
    }

    public void SetPrompt(Sprite glyph, string text)
    {
        GlyphImage.gameObject.SetActive(glyph != null);
        GlyphImage.sprite = glyph;
        PromptText.gameObject.SetActive(!string.IsNullOrWhiteSpace(text));
        PromptText.text = text;
    }

    public void Show(Vector3 worldPosition)
    {
        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
        }
        transform.position = worldPosition;
        gameObject.SetActive(true);
        FadeTo(1f);
    }

    public void Hide(Action onHidden, bool immediately = false)
    {
        FadeTo(0f, onHidden, immediately);
    }

    private void FadeTo(float targetAlpha, Action onHidden = null, bool immediately = false)
    {
        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
        }
        if (immediately)
        {
            Kill(targetAlpha, onHidden);
        }
        else
        {
            _fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha, onHidden));
        }
    }

    private IEnumerator FadeRoutine(float targetAlpha, Action onHidden = null)
    {
        float start = CanvasGroup.alpha;
        float time = 0f;
        while (time < FadeDuration)
        {
            time += Time.deltaTime;
            CanvasGroup.alpha = Mathf.Lerp(start, targetAlpha, time / FadeDuration);
            yield return null;
        }
        Kill(targetAlpha, onHidden);
    }

    private void Kill(float targetAlpha, Action onHidden)
    {
        CanvasGroup.alpha = targetAlpha;
        if (targetAlpha.IsNearZero())
        {
            gameObject.SetActive(false);
            onHidden?.Invoke();
        }
    }
}
