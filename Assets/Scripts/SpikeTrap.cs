using RobsonRocha.UnityCommon;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class SpikeTrap : Damaging
{
    [SerializeField] private Sprite openSpikesSprite;
    [SerializeField] private SoundEffect openSpikesSoundEffect;
    [SerializeField] private float intervalBeforeOpening = 0.5f;
    private float spikeOpeningTimer = 0f;

    [SerializeField] private Sprite closedSpikesSprite;
    [SerializeField] private SoundEffect closedSpikesSoundEffect;
    [SerializeField] private float intervalBeforeClosing = 3f;
    private float spikeClosingTimer = 0f;

    private bool _isOpen = false;
    private bool _isOpening = false;
    private bool _isClosing = false;

    private SpriteRenderer _renderer;
    private NearestDetector _damageablesDetector;
    private BoxCollider2D _boxCollider;

    private void Awake()
    {
        this.TryInitComponent(ref _renderer);
        this.TryInitComponent(ref _damageablesDetector);
        this.TryInitComponent(ref _boxCollider);
    }

    void Update()
    {
        if (_damageablesDetector.IsDetected)
        {
            HandleOpening();
        }
        else
        {
            HandleClosing();
        }
    }

    private void HandleOpening()
    {
        if (_isOpen) return; // Already open, nothing to do

        if (!_isOpening)
        {
            _isOpening = true;
            _isClosing = false;
            spikeOpeningTimer = intervalBeforeOpening;
            return;
        }

        /*spikeOpeningTimer = */spikeOpeningTimer.DecrementTimer();

        if (spikeOpeningTimer.IsAboveNearZero())
            return;

        // Timer reached zero, open the spikes
        _isOpen = true;
        _isOpening = false;
        SoundManager.Instance.PlaySfx(openSpikesSoundEffect);
        _renderer.sprite = openSpikesSprite;
        _boxCollider.enabled = true;
    }

    private void HandleClosing()
    {
        if (!_isOpen) return; // Already closed, nothing to do

        if (!_isClosing)
        {
            _isOpening = false;
            _isClosing = true;
            spikeClosingTimer = intervalBeforeClosing;
            return;
        }

        /*spikeClosingTimer = */spikeClosingTimer.DecrementTimer();

        if (spikeClosingTimer.IsAboveNearZero())
            return;

        // Timer reached zero, close the spikes
        _isOpen = false;
        _isClosing = false;
        SoundManager.Instance.PlaySfx(closedSpikesSoundEffect);
        _renderer.sprite = closedSpikesSprite;
        _boxCollider.enabled = false;
    }
}
