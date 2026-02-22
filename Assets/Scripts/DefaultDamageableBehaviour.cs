using RobsonRocha.UnityCommon;
using UnityEngine;

[RequireComponent(typeof(Damageable))]
[RequireComponent(typeof(FlashEffect))]
public class DefaultDamageableBehaviour : MonoBehaviour
{
    [SerializeField] private Transform InteractionPromptTransform;
    [SerializeField] private SoundEffect DamageTakenSoundEffect;
    [SerializeField] private SoundEffect HealingSoundEffect;

    private Damageable _damageable;
    private FlashEffect _flashEffect;

    private void Awake()
    {
        if (this.TryInitComponent(ref _damageable))
        {
            _damageable.TakingDamage += HandleDamageable_OnTakingDamage;
            _damageable.Healing += HandleDamageable_OnHealing;
        }
        this.TryInitComponent(ref _flashEffect);
    }

    protected virtual void OnTriggerStay2D(Collider2D collision) =>
        HandleDamaging(collision);

    private void OnDestroy()
    {
        if (_damageable != null)
        {
            _damageable.TakingDamage -= HandleDamageable_OnTakingDamage;
            _damageable.Healing -= HandleDamageable_OnHealing;
        }
    }

    private void HandleDamageable_OnTakingDamage(object sender, Damageable.TakingDamageEventArgs e)
    {
        PickupPopupManager.Instance.ShowPopup(
            text: $"-{e.Damage} HP",
            worldPosition: InteractionPromptTransform.position,
            color: Color.red,
            soundEffect: DamageTakenSoundEffect,
            immediate: true);

        if (_flashEffect != null)
        {
            _flashEffect.StartFlash(
                duration: _damageable.InvulnerabilityDurationAfterTakingDamage * 0.25f,
                color: Color.red);
            _flashEffect.StartBlinking(
                duration: _damageable.InvulnerabilityDurationAfterTakingDamage,
                color: Color.white);
        }
    }

    private void HandleDamageable_OnHealing(object sender, Damageable.HealingEventArgs e)
    {
        // Show a healing popup with the healing amount above the player's head
        PickupPopupManager.Instance.ShowPopup(
            text: $"+{e.Amount} HP",
            worldPosition: InteractionPromptTransform.position,
            color: Color.green,
            soundEffect: HealingSoundEffect,
            immediate: true);

        // Flash the sprite green briefly
        _flashEffect.StartFlash(
            duration: _damageable.InvulnerabilityDurationAfterTakingDamage * 0.5f,
            color: Color.green);
    }

    protected virtual void HandleDamaging(Collider2D collision)
    {
        if (_damageable != null)
        {
            if (collision.TryGetComponent(out Damaging damaging) && _damageable.CanTakeDamage())
            {
                damaging.DoDamage(_damageable);
            }
        }
    }
}
