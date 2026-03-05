using RobsonRocha.UnityCommon;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PixelText))]
[DefaultExecutionOrder(-85)]
public class InteractionPromptController : MonoBehaviour
{
    [SerializeField] private float FadeDuration = 0.05f;

    private Coroutine _fadeRoutine;
    private PixelText PromptText;

    private void Awake()
    {
        PromptText = GetComponent<PixelText>();
    }

    public void SetPrompt(Sprite glyph, string text)
    {
        PromptText.Render(text, additionalGlyphs: new List<Sprite> { glyph });
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
        float start = PromptText.Alpha;
        float time = 0f;
        while (time < FadeDuration)
        {
            time += Time.deltaTime;
            PromptText.SetAlpha(Mathf.Lerp(start, targetAlpha, time / FadeDuration));
            yield return null;
        }
        Kill(targetAlpha, onHidden);
    }

    private void Kill(float targetAlpha, Action onHidden)
    {
        PromptText.SetAlpha(targetAlpha);
        if (targetAlpha.IsNearZero())
        {
            gameObject.SetActive(false);
            onHidden?.Invoke();
        }
    }
}
