using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    #region Events
    public event Action<InventoryItem, int, string, bool> OnItemAdded;
    public event Action<InventoryItem, int, bool> OnItemRemoved;
    #endregion

    #region Fields
    private readonly Dictionary<string, int> _items = new();
    #endregion

    #region Inventory Management
    /// <summary>
    /// Adds a quantity of an item to the inventory.
    /// </summary>
    public void AddItem(InventoryItem item, int quantity = 1, string customMessage = null, bool silent = false)
    {
        if (quantity <= 0) return;

        if (_items.ContainsKey(item.Id))
        {
            _items[item.Id] += quantity;
        }
        else
        {
            _items[item.Id] = quantity;
        }

        OnItemAdded?.Invoke(item, quantity, customMessage, silent);
    }

    /// <summary>
    /// Removes a quantity of an item from the inventory. Returns true if successful.
    /// </summary>
    public bool RemoveItem(InventoryItem item, int quantity = 1, bool silent = false)
    {
        if (!HasEnough(item, quantity)) return false;

        _items[item.Id] -= quantity;

        if (_items[item.Id] <= 0)
        {
            _items.Remove(item.Id);
        }

        OnItemRemoved?.Invoke(item, quantity, silent);
        return true;
    }

    /// <summary>
    /// Checks if the inventory has enough of an item.
    /// </summary>
    public bool HasEnough(InventoryItem item, int quantity = 1)
    {
        return _items.ContainsKey(item.Id) && _items[item.Id] >= quantity;
    }

    /// <summary>
    /// Retrieves the quantity of the specified inventory item.
    /// </summary>
    /// <remarks>This method determines the quantity by using the item's unique identifier.</remarks>
    /// <param name="item">The inventory item for which to obtain the quantity. This parameter cannot be null.</param>
    /// <returns>The quantity of the specified inventory item. Returns 0 if the item does not exist in the inventory.</returns>
    public int GetQuantity(InventoryItem item) => 
        _items.ContainsKey(item.Id) ? _items[item.Id] : 0;

    /// <summary>
    /// Clears all items from the inventory.
    /// </summary>
    public void Clear()
    {
        _items.Clear();
    }

    /// <summary>
    /// Gets all items as key-value pairs (itemId, quantity).
    /// </summary>
    public IEnumerable<KeyValuePair<string, int>> GetAllItems() => _items;

    /// <summary>
    /// Gets the total number of unique items in the inventory.
    /// </summary>
    public int GetItemCount() => _items.Count;
    #endregion
}
