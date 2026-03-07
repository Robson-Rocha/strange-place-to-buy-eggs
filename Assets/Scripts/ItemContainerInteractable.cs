using RobsonRocha.UnityCommon;
using System;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ItemContainerInteractable : Interactable
{
    [Serializable]
    public class InventoryItemEntry
    {
        public InventoryItem Item;
        public int Quantity;
        public string CustomPickupPopupMessage;
    }

    [SerializeField] private string containerName;
    [SerializeField] private InventoryItemEntry[] items;
    [SerializeField] private InventoryItem requiredItem;
    [SerializeField] private SoundEffect openSoundEffect;
    [SerializeField] private bool consumeRequiredItem;
    [SerializeField] private int consumedQuantityFromRequiredItem;
    [SerializeField] private SoundEffect noRequiredItemSoundEffect;
    [SerializeField] private Sprite openedSprite;
    [SerializeField] private bool fadeAwayAfterOpen;
    [SerializeField] private float intervalBeforeFadeAway = 1f;

    private SpriteRenderer _spriteRenderer;

    private bool _hasBeenInteractedWith = false;

    private float _interactionCooldownTimer = 0f;

    protected override void Awake()
    {
        base.Awake();
        this.TryInitComponent(ref _spriteRenderer);
    }
    
    private void Update()
    {
        _interactionCooldownTimer.DecrementTimer();
    }

    public override string InteractionVerb => $"Open {containerName}";

    public override void Interact(GameObject interactor, Vector3 interactorTransformPosition)
    {
        // If this container has already been interacted with or if the interaction cooldown timer is still running, do nothing.
        if (_hasBeenInteractedWith || !_interactionCooldownTimer.IsNearZero()) return;

        // Try to get the Inventory component from the interactor. If it doesn't have one, do nothing.
        if (!interactor.TryGetComponent(out Inventory inventory))
            return;

        // If a required item is set, check if the player has enough of it. If not, show a popup and do nothing.
        // If consumeRequiredItem is true, remove the required item from the inventory.
        if (requiredItem != null)
        {
            int requiredQuantity = consumeRequiredItem && consumedQuantityFromRequiredItem > 0 ? consumedQuantityFromRequiredItem : 1;
            if (inventory.HasEnough(requiredItem, requiredQuantity))
            {
                if (consumeRequiredItem)
                {
                    inventory.RemoveItem(requiredItem, requiredQuantity);
                }
            }
            else
            {
                PickupPopupManager.Instance.ShowPopup($"You need {requiredQuantity} {(requiredQuantity > 1 ? requiredItem.PluralName : requiredItem.Name)} to open this.", interactorTransformPosition, soundEffect: noRequiredItemSoundEffect);
                _interactionCooldownTimer = 1f; // Set a cooldown to prevent spamming the interaction.
                return;
            }
        }

        // Mark as interacted to prevent further interactions and make it undetectable.
        _hasBeenInteractedWith = true;
        detectable.Undetectable = true;

        // Hide the interaction prompt immediately.
        InteractionPromptManager.Instance.HidePrompt(immediately: true);

        // Add items to the player's inventory or show a message if this container is empty.
        if (items == null || items.Length == 0)
        {
            PickupPopupManager.Instance.ShowPopup("It's empty...", interactorTransformPosition, soundEffect: openSoundEffect);
        }
        else
        {
            PickupPopupManager.Instance.ShowPopup($"The {name} opened!", interactorTransformPosition, soundEffect: openSoundEffect);
            foreach (InventoryItemEntry entry in items)
            {
                inventory.AddItem(entry.Item, entry.Quantity, entry.CustomPickupPopupMessage);
            }
        }

        // Change the sprite to the opened version if it's set.
        if (openedSprite != null)
        {
            _spriteRenderer.sprite = openedSprite;
        }

        // Start fading away and destroy the object after the specified interval if fadeAwayAfterOpen is true.
        if (fadeAwayAfterOpen)
        {
            this.FadeAndDestroy(intervalBeforeFadeAway);
        }
    }
}