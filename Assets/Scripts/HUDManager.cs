using RobsonRocha.UnityCommon;
using System;
using UnityEngine;

[DefaultExecutionOrder(-60)]
public class HUDManager : SingletonMonoBehaviour<HUDManager>
{
    public class ChangeTracker<T>
        where T : IEquatable<T>
    {
        private bool _hasChanges;
        private T _value;

        public void SetValue(T value)
        {
            _hasChanges = !_value.Equals(value);
            _value = value;
        }

        public T GetValue() => _value;

        public bool TryGetChangedValue(out T changedValue, bool force = false)
        {
            if (_hasChanges || force)
            {
                changedValue = _value;
                _hasChanges = false;
                return true;
            }
            changedValue = default;
            return false;
        }
    }

    public event Action<int> OnCoinsUpdated;
    public event Action<int, int> OnHeartsUpdated;

    private readonly ChangeTracker<int> _coinsValue = new();
    private readonly ChangeTracker<(int maxHealth, int currentHealth)> _healthValue = new();

    public void UpdateHud(Inventory inventory, Health health)
    {
        _coinsValue.SetValue(inventory.GetQuantity(InventoryManager.Instance.ItemsDatabase["Coin"]));
        _healthValue.SetValue((health.MaxHealth, health.CurrentHealth));
        RefreshHud();
    }

    public void RefreshHud(bool force = false)
    {
        if (OnCoinsUpdated != null && _coinsValue.TryGetChangedValue(out int newCoinsValue, force))
            OnCoinsUpdated?.Invoke(newCoinsValue);

        if (OnHeartsUpdated != null && _healthValue.TryGetChangedValue(out var newHealthValue, force))
            OnHeartsUpdated?.Invoke(newHealthValue.maxHealth, newHealthValue.currentHealth);
    }
}

