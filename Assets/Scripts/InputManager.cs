using RobsonRocha.UnityCommon;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
[DefaultExecutionOrder(-100)]
public class InputManager : SingletonMonoBehaviour<InputManager>
{
    [SerializeField] GlyphSet KeyboardGlyphSet;
    [SerializeField] GlyphSet GamepadGlyphSet;

    public const string MOVE_ACTION = "Move";
    public const string RUN_ACTION = "Run";
    public const string INTERACT_ACTION = "Interact";
    public const string ATTACK_ACTION = "Attack";
    public const string ROLL_ACTION = "Roll";

    public Vector2 Movement;
    public bool IsMoving;
    public bool IsRunning;
    public bool HasInteracted;
    public bool HasAttacked;
    public string CurrentControlScheme;

    private PlayerInput _playerInput;
    private InputAction _moveAction;
    private InputAction _runAction;
    private InputAction _interactAction;
    private InputAction _attackAction;

    // Awake is called when the script instance is being loaded
    protected override void Awake()
    {
        if (!base.CanAwake()) return;

        _playerInput = GetComponent<PlayerInput>();
        _playerInput.onControlsChanged += _ => UpdateCurrentControlScheme();
        _moveAction = _playerInput.actions[MOVE_ACTION];
        _runAction = _playerInput.actions[RUN_ACTION];
        _interactAction = _playerInput.actions[INTERACT_ACTION];
        _attackAction = _playerInput.actions[ATTACK_ACTION];
        UpdateCurrentControlScheme();
    }

    private void UpdateCurrentControlScheme()
    {
        CurrentControlScheme = _playerInput.currentControlScheme;
        Debug.Log($"Control scheme changed to: {CurrentControlScheme}");
    }

    // Update is called once per frame
    void Update()
    {
        Movement = _moveAction.ReadValue<Vector2>();
        IsMoving = !Movement.IsNearZero();
        IsRunning = _runAction.IsPressed();
        HasInteracted = _interactAction.WasPressedThisFrame();
        HasAttacked = _attackAction.WasPressedThisFrame();
    }

    public Sprite GetGlyph(string actionName, Direction? direction = null, bool alt = false)
    {
        GlyphSet glyphSet = CurrentControlScheme == "Gamepad" ? GamepadGlyphSet : KeyboardGlyphSet;

        switch (actionName)
        {
            case MOVE_ACTION:
                if (direction == null)
                {
                    Debug.LogWarning("Direction is required for Move action.");
                    return null;
                }
                if (direction.Value.IsUp())
                {
                    return alt ? glyphSet.UpAltGlyph : glyphSet.UpGlyph;
                }
                else if (direction.Value.IsDown())
                {
                    return alt ? glyphSet.DownAltGlyph : glyphSet.DownGlyph;
                }
                else if (direction.Value.IsLeft())
                {
                    return alt ? glyphSet.LeftAltGlyph : glyphSet.LeftGlyph;
                }
                else if (direction.Value.IsRight())
                {
                    return alt ? glyphSet.RightAltGlyph : glyphSet.RightGlyph;
                }
                break;
            case RUN_ACTION:
                return glyphSet.RunGlyph;
            case INTERACT_ACTION:
                return glyphSet.InteractGlyph;
            case ATTACK_ACTION:
                return glyphSet.AttackGlyph;
            case ROLL_ACTION:
                return glyphSet.RollGlyph;
            default:
                Debug.LogWarning($"No glyph found for action: {actionName}");
                return null;
        }

        return null;
    }
}
