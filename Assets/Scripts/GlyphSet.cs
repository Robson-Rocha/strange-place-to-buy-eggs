using UnityEngine;

[CreateAssetMenu(fileName = "GlyphSet", menuName = "Scriptable Objects/GlyphSet")]
public class GlyphSet : ScriptableObject
{
    public Sprite InteractGlyph; 
    public Sprite AttackGlyph; 
    public Sprite RunGlyph;
    public Sprite RollGlyph;
    public Sprite UpGlyph;
    public Sprite UpAltGlyph;
    public Sprite DownGlyph;
    public Sprite DownAltGlyph;
    public Sprite LeftGlyph;
    public Sprite LeftAltGlyph;
    public Sprite RightGlyph;
    public Sprite RightAltGlyph;
}
