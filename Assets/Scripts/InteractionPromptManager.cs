using RobsonRocha.UnityCommon;
using UnityEngine;

[DefaultExecutionOrder(-90)]
public class InteractionPromptManager : SingletonMonoBehaviour<InteractionPromptManager>
{
    [SerializeField] private InteractionPromptController PromptPrefab;

    private InteractionPromptController _activePrompt;
    private Transform _targetTransform;
    private Vector3 _offset;
    private string _lastInteractAction;
    private string _lastVerb;
    private bool _hiding = false, _hidden = true;

    protected override void Awake()
    {
        if (!base.CanAwake()) return;

        if (PromptPrefab != null)
        {
            _activePrompt = Instantiate(PromptPrefab);
            _activePrompt.Hide(ResetInteractionPrompt);
        }
    }

    public void ShowPrompt(string interactAction, string verb, Transform targetTransform, Vector3 offset = default)
    {
        if (_activePrompt == null || (interactAction == _lastInteractAction && verb == _lastVerb && !_hiding)) 
            return;
        _hiding = false; // Cancel any pending hide so its callback becomes a no-op
        Sprite glyph = InputManager.Instance.GetGlyph(interactAction);
        _lastInteractAction = interactAction;
        _lastVerb = verb;
        _targetTransform = targetTransform;
        _offset = offset;
        _hidden = false;
        _activePrompt.SetPrompt(glyph, $"%0 {verb}");
        _activePrompt.Show(targetTransform.position + offset);
    }

    public void HidePrompt(bool immediately = false)
    {
        if (_activePrompt == null || _hiding || _hidden) return;
        _hiding = true;
        _activePrompt.Hide(ResetInteractionPrompt, immediately);
    }

    private void ResetInteractionPrompt()
    {
        if (!_hiding) return; // A ShowPrompt call occurred after Hide started; ignore stale callback
        _targetTransform = null;
        _lastInteractAction = null;
        _lastVerb = null;
        _hiding = false;
        _hidden = true;
        _offset = default;
    }

    private void Update()
    {
        if (_activePrompt != null && _activePrompt.gameObject.activeSelf && _targetTransform != null)
        {
            _activePrompt.transform.position = _targetTransform.position + _offset;
        }
    }
}
