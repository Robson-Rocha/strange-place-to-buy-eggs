using RobsonRocha.UnityCommon;
using UnityEngine;

public class ItemCollectable : Collectable
{
    [SerializeField] private InventoryItem item;
    [SerializeField] private int quantity;
    [SerializeField] private string customPickupPopupMessage;
    [SerializeField] private SoundEffect soundEffect;

    public override void Collect(GameObject collector)
    {
        if (IsCollected) return;

        if (collector.TryGetComponent(out Inventory inventory))
        {
            IsCollected = true;
            inventory.AddItem(item, quantity, customPickupPopupMessage);
            this.FadeAndDestroy(0f);
        }
    }
}
