using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "PixelFont/FontData")]
public class PixelFontData : ScriptableObject
{
#if UNITY_EDITOR
    public event EventHandler FontDataChanged;
#endif

    [Tooltip("Path relative to Resources folder, e.g., 'PixelFonts/Dogica'")]
    [SerializeField] private string PathToResources = "PixelFonts/";

    [Tooltip("Prefix for sprite asset names, e.g., 'dogica' if your sprites are named like 'dogica_0', 'dogica_1', etc.")]
    [SerializeField] private string SpriteAssetResourceNamePrefix = "";

    [Tooltip("Characters corresponding to the sprites in order. The first character corresponds to the sprite with index 0, the second to index 1, and so on.")]
    [SerializeField] private string Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890!@#$%^&*()-+_=~ø[]{};:'\"\\|,<.>/?ÁÉÍÓÚáéíóúÇçÃÕãõÂÊÎÔÛâêîôûÄËÏÖÜäëïöüÀÈÌÒÙàèìòù¡‘’×Åå«»Ññ¿`´ßæ©";

    [Tooltip("Number of pixels per character in the sprite sheet.")]
    public int PixelsPerCharacter = 8;

    private Dictionary<char, Sprite> lookup;

    public Sprite GetSprite(char c)
    {
        EnsureLookupInitialized();
        return lookup.TryGetValue(c, out Sprite sprite) ? sprite : null;
    }

    private void EnsureLookupInitialized()
    {
        if (lookup != null)
            return;

        lookup = new Dictionary<char, Sprite>();

        string basePath = PathToResources.TrimEnd('/');
        string spritePath = $"{basePath}/{SpriteAssetResourceNamePrefix}";
        Sprite[] sprites = Resources.LoadAll<Sprite>(spritePath);

        foreach (Sprite sprite in sprites)
        {
            string[] parts = sprite.name.Split('_');
            if (parts.Length >= 2 && int.TryParse(parts[^1], out int index) && index < Characters.Length)
            {
                lookup[Characters[index]] = sprite;
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        lookup = null;
        FontDataChanged?.Invoke(this, EventArgs.Empty);
    }
#endif
}
