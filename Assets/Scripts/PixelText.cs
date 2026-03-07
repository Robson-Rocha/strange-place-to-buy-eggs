using System.Collections.Generic;
using UnityEngine;

public class PixelText : MonoBehaviour
{
    [Header("Font & Content")]
    [SerializeField] private PixelFontData font;
    [SerializeField] private string Text;

    [Header("Style")]
    [SerializeField] private Color TextColor = Color.white;
    [SerializeField] private bool UseOutline = true;
    [SerializeField] private Color OutlineColor = Color.black;
    [Range(0f, 1f)] public float Alpha = 1f;

    [Header("Layout")]
    [SerializeField] private TextAlignment Alignment = TextAlignment.Center;
    [Tooltip("Letter spacing in pixels")]
    [SerializeField] private int LetterSpacing = 1;
    [Tooltip("Pixels per unit for layout calculations")]
    [SerializeField] private float PixelsPerUnit = 16f;

    [Header("Rendering")]
    [SerializeField] private string SortingLayer = "Default";
    [SerializeField] private int OrderInLayer = 0;
    [SerializeField] private uint RenderingLayerMask = 1;

    // Cache sprite renderers to avoid GetComponentsInChildren allocations
    private readonly List<SpriteRenderer> _cachedRenderers = new();
    private static MaterialPropertyBlock _propertyBlock;

    private static MaterialPropertyBlock PropertyBlock
    {
        get
        {
            if (_propertyBlock == null)
                _propertyBlock = new MaterialPropertyBlock();
            return _propertyBlock;
        }
    }

    public void SetAlpha(float alpha)
    {
        Alpha = Mathf.Clamp01(alpha);

        // Use cached renderers instead of GetComponentsInChildren
        for (int i = 0; i < _cachedRenderers.Count; i++)
        {
            SpriteRenderer sr = _cachedRenderers[i];
            if (sr != null)
            {
                sr.GetPropertyBlock(PropertyBlock);
                Color c = PropertyBlock.GetColor("_Color");
                c.a = Alpha;
                PropertyBlock.SetColor("_Color", c);
                sr.SetPropertyBlock(PropertyBlock);
            }
        }
    }

    public void Render(
        string text = null,
        List<Sprite> additionalGlyphs = null,
        Color? textColor = null,
        bool? useOutline = null,
        Color? outlineColor = null,
        TextAlignment? alignment = null,
        int? letterSpacing = null,
        float? alpha = null)
    {
        Render(
            text ?? Text,
            additionalGlyphs,
            textColor ?? TextColor,
            useOutline ?? UseOutline,
            outlineColor ?? OutlineColor,
            alignment ?? Alignment,
            letterSpacing ?? LetterSpacing,
            alpha ?? Alpha);
    }

    private void Render(
        string text,
        List<Sprite> additionalGlyphs,
        Color textColor,
        bool useOutline,
        Color outlineColor,
        TextAlignment alignment,
        int letterSpacing,
        float alpha)
    {
        ClearChildren();

        if (string.IsNullOrEmpty(text) || font == null)
            return;

        float letterSpacingWorld = letterSpacing / PixelsPerUnit;
        float x = 0f;

        for (int i = 0, l = text.Length; i < l; i++)
        {
            char c = text[i];
            string objectName = c.ToString();
            Sprite s = null;

            if (c != ' ')
            {
                if (c == '%' && additionalGlyphs != null && i + 1 < l && char.IsDigit(text[i + 1]))
                {
                    int index = text[i + 1] - '0';
                    if (index >= 0 && index < additionalGlyphs.Count)
                    {
                        s = additionalGlyphs[index];
                        objectName = $"Glyph{index}";
                        i++;
                    }
                }

                if (s == null)
                {
                    s = font.GetSprite(c);
                }
            }

            if (s == null)
            {
                float charWidth = font.PixelsPerCharacter / PixelsPerUnit;
                x += charWidth + letterSpacingWorld;
                continue;
            }

            x += RenderSprite(s, objectName, x, textColor, outlineColor, useOutline, letterSpacingWorld, alpha);
        }

        ApplyAlignment(x - letterSpacingWorld, alignment);
    }

    private void ClearChildren()
    {
        _cachedRenderers.Clear(); // Clear cache when clearing children
        
        int childCount = transform.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(child);
            else
                Destroy(child);
#else
            Destroy(child);
#endif
        }
    }

    private float RenderSprite(
        Sprite s,
        string objectName,
        float xPosition,
        Color textColor,
        Color outlineColor,
        bool useOutline,
        float finalLetterSpacingWorld,
        float alpha)
    {
        float pixel = 1f / s.pixelsPerUnit;

        GameObject letterParent = new(objectName);
        letterParent.transform.SetParent(transform);
        letterParent.transform.localPosition = new Vector3(xPosition, 0, 0);

        if (useOutline)
        {
            CreateOutline(letterParent.transform, s, pixel, outlineColor, alpha);
        }

        GameObject main = new("Main");
        main.transform.SetParent(letterParent.transform);
        main.transform.localPosition = Vector3.zero;

        CreateSpriteRenderer(main, s, textColor, useOutline ? 5 : 0, alpha);

        float spriteWidth = s.rect.width / s.pixelsPerUnit;
        float outlineWidth = useOutline ? pixel * 2f : 0f; // Outline adds 1 pixel on each side
        
        return spriteWidth + outlineWidth + finalLetterSpacingWorld;
    }

    private void CreateOutline(Transform parent, Sprite s, float pixel, Color finalOutlineColor, float alpha)
    {
        Vector3[] offsets = new Vector3[]
        {
            new(-pixel, 0, 0),
            new(pixel, 0, 0),
            new(0, pixel, 0),
            new(0, -pixel, 0)
        };

        int sortIndex = 0;
        foreach (var off in offsets)
        {
            GameObject o = new("Outline");
            o.transform.SetParent(parent);
            o.transform.localPosition = off;

            CreateSpriteRenderer(o, s, finalOutlineColor, sortIndex++, alpha);
        }
    }

    private void CreateSpriteRenderer(GameObject parent, Sprite s, Color color, int sortingOrderOffset, float alpha)
    {
        var sr = parent.AddComponent<SpriteRenderer>();
        sr.sprite = s;

        // Use MaterialPropertyBlock to set color WITHOUT creating material instance
        color.a = alpha;
        PropertyBlock.SetColor("_Color", color);
        sr.SetPropertyBlock(PropertyBlock);

        sr.sortingLayerName = SortingLayer;
        sr.sortingOrder = OrderInLayer + sortingOrderOffset;
        sr.renderingLayerMask = RenderingLayerMask;

        _cachedRenderers.Add(sr);
    }

    private void ApplyAlignment(float totalWidth, TextAlignment targetAlignment)
    {
        if (totalWidth <= 0)
            return;

        float offset = targetAlignment switch
        {
            TextAlignment.Left => 0f,
            TextAlignment.Center => -totalWidth / 2f,
            TextAlignment.Right => -totalWidth,
            _ => 0f
        };

        if (offset == 0f)
            return;

        foreach (Transform child in transform)
        {
            Vector3 pos = child.localPosition;
            pos.x += offset;
            child.localPosition = pos;
        }
    }

#if UNITY_EDITOR
    private PixelFontData previousFont;
    private bool needsUpdate = false;

    private void OnEnable()
    {
        SubscribeToFontEvents();
    }

    private void OnDisable()
    {
        UnsubscribeFromFontEvents();
    }

    private void OnValidate()
    {
        HandleFontChange();
        NeedsUpdate();
    }

    private void NeedsUpdate()
    {
        needsUpdate = true;
        UnityEditor.EditorApplication.update -= EditorUpdate;
        UnityEditor.EditorApplication.update += EditorUpdate;
    }

    private void HandleFontChange()
    {
        if (previousFont != font)
        {
            UnsubscribeFromFontEvents();
            SubscribeToFontEvents();
            previousFont = font;
        }
    }

    private void SubscribeToFontEvents()
    {
        if (font != null)
        {
            font.FontDataChanged -= OnFontDataChanged;
            font.FontDataChanged += OnFontDataChanged;
        }
    }

    private void UnsubscribeFromFontEvents()
    {
        if (previousFont != null)
        {
            previousFont.FontDataChanged -= OnFontDataChanged;
        }
    }

    private void OnFontDataChanged(object sender, System.EventArgs e)
    {
        NeedsUpdate();
    }

    private void EditorUpdate()
    {
        if (!this) return; // Component has been destroyed
        
        if (!needsUpdate) return;

        needsUpdate = false;
        UnityEditor.EditorApplication.update -= EditorUpdate;

        if (!Application.isPlaying)
            Render();
    }
#endif

}
