using RobsonRocha.UnityCommon;
using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class Player : MonoBehaviour
{
    #region Movement & Animation Fields
    private const float MOVE_SPEED = 5f;
    private const float RUN_SPEED = 12f;
    private const float ATTACK_SPEED_MULTIPLIER = 0.5f;
    private Rigidbody2D _rb;
    private AnimatorParameterBinder _parameterBinder;
    private Animator _animator;
    private const string ANIM_PARAM_IS_IDLING = "IsIdling";
    private const string ANIM_PARAM_IS_MOVING = "IsMoving";
    private const string ANIM_PARAM_IS_RUNNING = "IsRunning";
    private const string ANIM_PARAM_IS_FACING_UP = "IsFacingUp";
    private const string ANIM_PARAM_IS_FACING_RIGHT = "IsFacingRight";
    private const string ANIM_PARAM_IS_FACING_DOWN = "IsFacingDown";
    private const string ANIM_PARAM_IS_FACING_LEFT = "IsFacingLeft";
    private const string ANIM_PARAM_IS_KNOCKING_BACK = "IsKnockingBack";
    private const string ANIM_PARAM_IS_ATTACKING = "IsAttacking";
    private float _lastHorizontal = 0;
    private float _lastVertical = -1; // Default to down-facing sprite
    private Vector2 _desiredVelocity;
    #endregion

    #region Attack Fields
    [SerializeField] private BoxCollider2D DamagingCollider;
    private bool _isAttacking = false;
    private Vector2 _attackDirection;
    private float _attackSpeed;
    #endregion

    #region State Properties (Mutually Exclusive)
    private bool IsKnockingBack => _knockbackable != null && _knockbackable.IsKnockingBack;
    private bool IsAttacking => _isAttacking && !IsKnockingBack;
    private bool IsRunning => !IsKnockingBack && !IsAttacking && InputManager.Instance.IsRunning && InputManager.Instance.IsMoving;
    private bool IsMoving => !IsKnockingBack && !IsAttacking && !IsRunning && InputManager.Instance.IsMoving;
    private bool IsIdling => !IsKnockingBack && !IsAttacking && !IsRunning && !IsMoving;
    #endregion

    #region Facing Direction Properties
    private bool IsFacingUp => _lastVertical > 0;
    private bool IsFacingDown => _lastVertical < 0;
    private bool IsFacingRight => _lastHorizontal > 0;
    private bool IsFacingLeft => _lastHorizontal < 0;
    #endregion

    #region Interaction Fields
    [SerializeField] private Transform InteractionPromptTransform;
    private NearestDetector _interactablesDetector;
    #endregion

    #region Inventory Fields
    private Inventory _inventory;
    private InventoryItem _coin;
    #endregion

    #region Damageable, Damaging and Knockbackable Fields
    private Damageable _damageable;
    private Knockbackable _knockbackable;
    #endregion

    #region Unity Messages
    // Awake is called when the script instance is being loaded
    void Awake()
    {
        // Movement and Animation
        this.TryInitComponent(ref _rb);
        this.TryInitComponent(ref _animator);
        if (this.TryInitComponent(ref _parameterBinder))
        {
            _parameterBinder.Bind(ANIM_PARAM_IS_IDLING, () => IsIdling);
            _parameterBinder.Bind(ANIM_PARAM_IS_MOVING, () => IsMoving);
            _parameterBinder.Bind(ANIM_PARAM_IS_RUNNING, () => IsRunning);
            _parameterBinder.Bind(ANIM_PARAM_IS_FACING_UP, () => IsFacingUp);
            _parameterBinder.Bind(ANIM_PARAM_IS_FACING_RIGHT, () => IsFacingRight);
            _parameterBinder.Bind(ANIM_PARAM_IS_FACING_DOWN, () => IsFacingDown);
            _parameterBinder.Bind(ANIM_PARAM_IS_FACING_LEFT, () => IsFacingLeft);
            _parameterBinder.Bind(ANIM_PARAM_IS_ATTACKING, () => IsAttacking);
        }
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Interaction
        this.TryInitComponent(ref _interactablesDetector, t => t.Name == "InteractablesDetector");

        // Inventory
        if (this.TryInitComponent(ref _inventory))
        {
            _inventory.OnItemAdded += HandleInventory_OnItemAdded;
            _inventory.OnItemRemoved += HandleInventory_OnItemRemoved;
            _coin = InventoryManager.Instance.ItemsDatabase["Coin"];
        }

        // Damageable and Knockbackable
        if(this.TryInitComponent(ref _damageable))
        {
            _damageable.CurrentHealthChanged += HandleDamageable_OnCurrentHealthChanged;
            _damageable.TakingDamage += HandleDamageable_OnTakingDamage;
            _damageable.Death += HandleDamageable_OnDeath;
        }
        if(this.TryInitComponent(ref _knockbackable))
        {
            if (_parameterBinder != null)
            {
                _parameterBinder.Bind(ANIM_PARAM_IS_KNOCKING_BACK, () => IsKnockingBack);
            }
        }
    }

    private void Start()
    {
        if (_inventory != null)
        {
            _inventory.AddItem(_coin, 5, silent: true);
        }
    }

    // Update is called once per frame
    void Update()
    {
        HandleAttacking();
        HandleMovement();
        HandleInteractions();
    }

    // FixedUpdate is called at a fixed interval and is independent of frame rate. Put physics code here.
    void FixedUpdate()
    {
        _rb.linearVelocity = _desiredVelocity;
    }

    // This method is called when the Collider2D other enters the trigger (if the GameObject has a Collider2D component with "Is Trigger" checked)
    void OnTriggerEnter2D(Collider2D collision)
    {
        HandleCollectables(collision);
    }

    // This method is called when the GameObject is destroyed
    private void OnDestroy()
    {
        if (_inventory != null)
        {
            _inventory.OnItemAdded -= HandleInventory_OnItemAdded;
            _inventory.OnItemRemoved -= HandleInventory_OnItemRemoved;
        }
        if (_damageable != null)
        {
            _damageable.CurrentHealthChanged -= HandleDamageable_OnCurrentHealthChanged;
            _damageable.TakingDamage -= HandleDamageable_OnTakingDamage;
            _damageable.Death -= HandleDamageable_OnDeath;
        }

    }
    #endregion

    #region Attack Handling Methods
    private void HandleAttacking()
    {
        // Knockback cancels attack
        if (IsKnockingBack)
        {
            _isAttacking = false;
        }
        // Start attack if attack button pressed and not already attacking
        else if (InputManager.Instance.HasAttacked && !IsAttacking)
        {
            _isAttacking = true;

            // Lock direction and speed at moment of attack
            _attackDirection = new Vector2(_lastHorizontal, _lastVertical).normalized;
            _attackSpeed = _desiredVelocity.magnitude * ATTACK_SPEED_MULTIPLIER;
        }

        // Enable the damaging collider only during the attack
        DamagingCollider.enabled = _isAttacking;
    }

    /// <summary>
    /// Called by the attack animation event on the last frame to signal the attack has finished.
    /// </summary>
    public void OnAttackAnimationFinished()
    {
        _isAttacking = false;
    }
    #endregion

    #region Movement Handling Methods
    private void HandleMovement()
    {
        // During knockback, use knockback velocity and face the attacker
        if (IsKnockingBack)
        {
            _desiredVelocity = _knockbackable.GetCurrentVelocity();
            _lastHorizontal = _knockbackable.FacingDirection.x;
            _lastVertical = _knockbackable.FacingDirection.y;
        }
        // During attack, use locked direction and half speed
        else if (IsAttacking)
        {
            _desiredVelocity = _attackDirection * _attackSpeed;
            // Keep facing the attack direction (don't update _lastHorizontal/_lastVertical)
        }
        // When not knocking back or attacking, use player input for movement and facing direction
        else if (InputManager.Instance.IsMoving)
        {
            _lastHorizontal = InputManager.Instance.Movement.x;
            _lastVertical = InputManager.Instance.Movement.y;
            _desiredVelocity = InputManager.Instance.Movement * (InputManager.Instance.IsRunning ? RUN_SPEED : MOVE_SPEED);
        }
        // If not moving, set velocity to zero but keep the last facing direction for idle animation
        else
        {
            _desiredVelocity = Vector2.zero;
        }
    }
    #endregion

    #region Interaction Handling Methods
    private void HandleInteractions()
    {
        if (_interactablesDetector.IsDetected &&
            _interactablesDetector.Target.TryGetComponent(out Interactable interactable))
        {
            // Show the glyph to interact with the nearest interactable on the top of the head of the player
            InteractionPromptManager.Instance.ShowPrompt(
                InputManager.INTERACT_ACTION,
                interactable.InteractionVerb,
                InteractionPromptTransform);

            // If the player presses the interact button, interact with the nearest interactable
            if (InputManager.Instance.HasInteracted)
            {
                interactable.Interact(gameObject, InteractionPromptTransform.position);
            }
        }
        else
        {
            // Hide the glyph to interact
            InteractionPromptManager.Instance.HidePrompt();
        }
    }
    #endregion

    #region Collection Handling Methods
    void HandleCollectables(Collider2D collision)
    {
        if (collision.TryGetComponent(out Collectable collectable))
        {
            if (collectable.IsCollected) return;
            collectable.Collect(gameObject);
        }
    }
    #endregion

    #region Inventory Handling Methods and Events
    void HandleInventory_OnItemAdded(InventoryItem item, int quantity, string customMessage = null, bool silent = false)
    {
        if (!silent)
        {
            // Show a pickup popup with the item name and quantity above the player's head
            PickupPopupManager.Instance.ShowPopup(
                string.IsNullOrEmpty(customMessage) ?
                    $"+{quantity} {(quantity > 1 ? item.PluralName : item.Name)}" :
                customMessage,
            InteractionPromptTransform.position,
            item.PickupPopupColor,
            soundEffect: item.PickupSoundEffect);
        }

        // Update the HUD
        HUDManager.Instance.UpdateHud(_inventory, _damageable.Health);
    }

    void HandleInventory_OnItemRemoved(InventoryItem item, int quantity, bool silent = false)
    {
        if (!silent)
        {
            // Show a pickup popup with the item name and quantity above the player's head
            PickupPopupManager.Instance.ShowPopup(
                $"-{quantity} {(quantity > 1 ? item.PluralName : item.Name)}", 
                InteractionPromptTransform.position,
            Color.red,
            soundEffect: null);
        }

        // Update the HUD
        HUDManager.Instance.UpdateHud(_inventory, _damageable.Health);
    }
    #endregion

    #region Damageable, Damaging and Knockbackable Methods and Events
    void HandleDamageable_OnCurrentHealthChanged(object sender, Damageable.HealthChangedEventArgs e)
    {
        HUDManager.Instance.UpdateHud(_inventory, e.Health);
    }

    void HandleDamageable_OnTakingDamage(object sender, Damageable.TakingDamageEventArgs e)
    {
        // Start knockback effect
        if (_knockbackable != null)
        {
            _knockbackable.StartKnockback(e.SourcePosition);
        }
    }

    void HandleDamageable_OnDeath(object sender, EventArgs e)
    {
        // Instantly heal the player to full health (for now, we will implement a proper death and respawn system later)
        _damageable.Heal(_damageable.Health.MaxHealth);

        // Show a (not) death popup above the player's head
        PickupPopupManager.Instance.ShowPopup(
            "You Cannot Die... Yet!",
            InteractionPromptTransform.position,
            Color.cyan);
    }
    #endregion
}
