using RobsonRocha.UnityCommon;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Damageable))]
public class DefaultDamageableHandling : MonoBehaviour
{
    [SerializeField] private DamageReceivingCollider[] DamageReceivingColliders;

    [SerializeField] private Transform InteractionPromptTransform;
    [SerializeField] private SoundEffect DamageTakenSoundEffect;
    [SerializeField] private SoundEffect HealingSoundEffect;

    [SerializeField] private bool ShouldShowPopup = true;
    [SerializeField] private bool ShouldBlink = true;
    [SerializeField] private bool ShouldFlash = true;

    private Damageable _damageable;
    private FlashEffect _flashEffect;
    
    // Cache overlapping damaging sources to avoid TryGetComponent every frame
    private readonly HashSet<Damaging> _overlappingDamagingSources = new();

    private void Awake()
    {
        if (this.TryInitComponent(ref _damageable))
        {
            _damageable.TakingDamage += HandleDamageable_OnTakingDamage;
            _damageable.Healing += HandleDamageable_OnHealing;
        }
        this.TryInitComponent(ref _flashEffect, isOptional: true);
    }

    private void OnTriggerEnter2D(Collider2D damagingCollider)
    {
        // Cache the Damaging component when entering
        if (damagingCollider.TryGetComponent(out Damaging damaging) &&
            IsDamagingAnyReceivingColliders(damagingCollider, damaging.KindOfDamageCaused))
        {
            _overlappingDamagingSources.Add(damaging);
        }
    }

    private void OnTriggerExit2D(Collider2D damagingCollider)
    {
        // Remove from cache when exiting
        if (damagingCollider.TryGetComponent(out Damaging damaging))
        {
            _overlappingDamagingSources.Remove(damaging);
        }
    }

    private void FixedUpdate()
    {
        // Process damage once per physics frame for all cached sources
        if (_damageable != null && _damageable.CanTakeDamage())
        {
            foreach (Damaging damaging in _overlappingDamagingSources)
            {
                if (damaging != null && damaging.enabled) // Null check in case destroyed
                {
                    damaging.DoDamage(_damageable);
                    break; // Only take damage from one source per frame
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (_damageable != null)
        {
            _damageable.TakingDamage -= HandleDamageable_OnTakingDamage;
            _damageable.Healing -= HandleDamageable_OnHealing;
        }
    }
    private bool IsDamagingAnyReceivingColliders(Collider2D damagingCollider, string kindOfDamageCaused)
    {
        if (string.IsNullOrWhiteSpace(kindOfDamageCaused))
            return true; // If the damaging source doesn't specify a kind, consider it as damaging all colliders

        if (damagingCollider == null)
            return false;

        if (DamageReceivingColliders == null || DamageReceivingColliders.Length == 0)
            return true; // Backward-compatible fallback

        foreach (DamageReceivingCollider damageReceivingCollider in DamageReceivingColliders)
        {
            if (damageReceivingCollider != null && 
                damageReceivingCollider.Collider != null && 
                (string.IsNullOrWhiteSpace(damageReceivingCollider.KindOfDamageReceived) || damageReceivingCollider.KindOfDamageReceived == kindOfDamageCaused) &&
                damageReceivingCollider.Collider.IsTouching(damagingCollider))
                return true;
        }

        return false;
    }
    private void HandleDamageable_OnTakingDamage(object sender, Damageable.TakingDamageEventArgs e)
    {
        if (ShouldShowPopup && InteractionPromptTransform != null)
        {
            PickupPopupManager.Instance.ShowPopup(
                text: $"-{e.Damage} HP",
                worldPosition: InteractionPromptTransform.position,
                color: Color.red,
                soundEffect: DamageTakenSoundEffect);
        }

        if (_flashEffect != null)
        {
            if (ShouldFlash)
            {
                _flashEffect.StartFlash(
                    duration: _damageable.InvulnerabilityDurationAfterTakingDamage * 0.25f);
            }
            if (ShouldBlink)
            {
                _flashEffect.StartBlinking(
                    duration: _damageable.InvulnerabilityDurationAfterTakingDamage);
            }
        }
    }

    private void HandleDamageable_OnHealing(object sender, Damageable.HealingEventArgs e)
    {
        // Show a healing popup with the healing amount above the player's head
        if (ShouldShowPopup && InteractionPromptTransform != null)
        {
            PickupPopupManager.Instance.ShowPopup(
                text: $"+{e.Amount} HP",
                worldPosition: InteractionPromptTransform.position,
                color: Color.green,
                soundEffect: HealingSoundEffect);
        }

        // Flash the sprite green briefly
        if (_flashEffect != null && ShouldFlash)
        {
            _flashEffect.StartFlash(
                duration: _damageable.InvulnerabilityDurationAfterTakingDamage * 0.5f,
                color: Color.green);
        }
    }
}
