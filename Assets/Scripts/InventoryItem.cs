using RobsonRocha.UnityCommon;
using UnityEngine;

[CreateAssetMenu(fileName = "New Inventory Item", menuName = "Inventory/Item")]
public class InventoryItem : ScriptableObject
{
    public string Id;
    public string Name;
    public string PluralName;
    public string Description;
    public Sprite Icon;
    public Color PickupPopupColor = Color.yellow;
    public SoundEffect PickupSoundEffect;
}
