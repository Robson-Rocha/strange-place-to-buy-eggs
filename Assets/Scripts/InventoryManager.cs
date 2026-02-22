using RobsonRocha.UnityCommon;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-80)]
public class InventoryManager : SingletonMonoBehaviour<InventoryManager>
{
    public class InventoryItemDatabase
    {
        private readonly Dictionary<string, InventoryItem> _inventoryItems = new();

        public InventoryItem this[string inventoryItemId]
        {
            get
            {
                if (!_inventoryItems.TryGetValue(inventoryItemId, out InventoryItem inventoryItem))
                {
                    inventoryItem = Resources.Load<InventoryItem>($"InventoryItems/{inventoryItemId}");
                    if (inventoryItem != null)
                    {
                        _inventoryItems[inventoryItemId] = inventoryItem;
                    }
                    else
                    {
                        Debug.LogError($"InventoryItem with ID '{inventoryItemId}' not found.");
                    }
                }
                return inventoryItem;
            }
        }
    }

    public enum TransactionResult
    {
        Success,
        SellerDoesNotHaveEnough,
        BuyerDoesNotHaveEnough
    }

    public InventoryItemDatabase ItemsDatabase { get; } = new();

    public bool TryTransaction(
        Inventory sellerInventory, Inventory buyerInventory, 
        InventoryItem itemBeingSold, int quantityBeingSold, 
        InventoryItem itemBeingRequested, int quantityBeingRequested,
        out TransactionResult transactionResult)
    {
        if (!sellerInventory.HasEnough(itemBeingSold, quantityBeingSold))
        {
            transactionResult = TransactionResult.SellerDoesNotHaveEnough;
            return false;
        }
        if (!buyerInventory.HasEnough(itemBeingRequested, quantityBeingRequested))
        {
            transactionResult = TransactionResult.BuyerDoesNotHaveEnough;
            return false;
        }
        sellerInventory.RemoveItem(itemBeingSold, quantityBeingSold);
        buyerInventory.AddItem(itemBeingSold, quantityBeingSold);
        buyerInventory.RemoveItem(itemBeingRequested, quantityBeingRequested);
        sellerInventory.AddItem(itemBeingRequested, quantityBeingRequested);
        transactionResult = TransactionResult.Success;
        return true;
    }
}

