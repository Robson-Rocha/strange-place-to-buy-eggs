using RobsonRocha.UnityCommon;
using System;
using System.ComponentModel;
using UnityEngine;

public class Damageable : MonoBehaviour
{
    public class TakingDamageEventArgs : CancelEventArgs
    {
        public TakingDamageEventArgs(Health health, int damage, Vector3 sourcePosition, string kind)
        {
            Health = health;
            Damage = damage;
            SourcePosition = sourcePosition;
            Kind = kind;
        }

        public virtual Health Health { get; }
        public virtual int Damage { get; set; }
        public virtual Vector3 SourcePosition { get; }
        public virtual string Kind { get; }
    }

    public class HealingEventArgs : CancelEventArgs
    {
        public HealingEventArgs(Health health, int amount)
        {
            Health = health;
            Amount = amount;
        }

        public virtual Health Health { get; }
        public virtual int Amount { get; set; }
    }

    public class HealthChangedEventArgs : EventArgs
    {
        public HealthChangedEventArgs(Health health) => Health = health;
        public virtual Health Health { get; }
    }

    public Health Health;

    public float InvulnerabilityDurationAfterTakingDamage = 3f;
    private float _invulnerabilityTimer = 0f;

    public event EventHandler<TakingDamageEventArgs> TakingDamage;
    public event EventHandler<HealingEventArgs> Healing;
    public event EventHandler<HealthChangedEventArgs> CurrentHealthChanged;
    public event EventHandler Death;

    private void Awake()
    {
        if (TryGetComponent(out Detectable detectable))
        {
            detectable.Names.AddIfNotExists(nameof(Damageable));
        }
    }

    void Update() =>
        /*_invulnerabilityTimer = */_invulnerabilityTimer.DecrementTimer();

    public bool CanTakeDamage() =>
        _invulnerabilityTimer.IsNearZero();

    public void TakeDamage(int damage, Vector3 sourcePosition, string kind)
    {
        if (damage <= 0 || Health == null || !CanTakeDamage())
            return;

        if (TakingDamage != null)
        {
            TakingDamageEventArgs takingDamageEventArgs = 
                OnTakingDamage(Health, damage, sourcePosition, kind);

            if (takingDamageEventArgs.Cancel)
                return;

            damage = takingDamageEventArgs.Damage;
        }

        _invulnerabilityTimer = InvulnerabilityDurationAfterTakingDamage;

        Health.CurrentHealth = 
            Math.Max(Health.CurrentHealth - damage, 0);
        
        OnCurrentHealthChanged(Health);

        if (Health.CurrentHealth == 0)
            OnDeath();
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || Health == null || Health.CurrentHealth >= Health.MaxHealth)
            return;

        if (Healing != null)
        {
            HealingEventArgs healingEventArgs = OnHealing(Health, amount);

            if (healingEventArgs.Cancel)
                return;

            amount = healingEventArgs.Amount;
        }

        Health.CurrentHealth =
            Math.Min(Health.CurrentHealth + amount, Health.MaxHealth);

        OnCurrentHealthChanged(Health);
    }

    protected virtual TakingDamageEventArgs OnTakingDamage(
        Health health, int damage, Vector3 sourcePosition, string kind)
    {
        TakingDamageEventArgs eventArgs = new(Health, damage, sourcePosition, kind);
        TakingDamage?.Invoke(this, eventArgs);
        return eventArgs;
    }

    protected virtual HealingEventArgs OnHealing(Health health, int amount)
    {
        HealingEventArgs eventArgs = new(Health, amount);
        Healing?.Invoke(this, eventArgs);
        return eventArgs;
    }

    protected virtual void OnCurrentHealthChanged(Health health)
        => CurrentHealthChanged?.Invoke(this, new HealthChangedEventArgs(health));

    protected virtual void OnDeath()
        => Death?.Invoke(this, EventArgs.Empty);
}