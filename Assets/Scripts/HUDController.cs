using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

[RequireComponent(typeof(UIDocument))]
[DefaultExecutionOrder(-55)]
public class HUDController : MonoBehaviour
{
    [SerializeField] Sprite EmptyHeartSprite;
    [SerializeField] Sprite FullHeartSprite;

    private UIDocument _hudUIDocument;
    private VisualElement _root;
    private Label _coinsLabel;
    private VisualElement _heartsPanel;
    private readonly Image[] _heartImages = new Image[20];

    void Start()
    {
        _hudUIDocument = GetComponent<UIDocument>();
        _root = _hudUIDocument.rootVisualElement;
        
        _heartsPanel = _root.Q<VisualElement>("HeartsPanel");
        _coinsLabel = _root.Q<Label>("CoinsLabel");

        for (int i = 0; i < _heartImages.Length; i++)
        {
            _heartImages[i] = _heartsPanel[i] as Image;
        }

        HUDManager.Instance.OnCoinsUpdated += OnCoinsUpdated;
        HUDManager.Instance.OnHeartsUpdated += OnHeartsUpdated;
        HUDManager.Instance.RefreshHud(force: true);
    }

    void OnCoinsUpdated(int coins)
    {
        if (_coinsLabel != null)
            _coinsLabel.text = coins.ToString("D5");
    }

    void OnHeartsUpdated(int maxMearts, int currentHearts)
    {
        // Iterate from 0 to 19, making visible up to the maxHearts (invisible afterwards), and filling up to the currentHearts (empty afterwads).
        for (int i = 0; i < _heartImages.Length; i++)
        {
            if (i < maxMearts)
            {
                _heartImages[i].style.display = DisplayStyle.Flex;
                Sprite previousSprite = _heartImages[i].sprite;
                bool shouldBeFull = i < currentHearts;
                _heartImages[i].sprite = shouldBeFull ? FullHeartSprite : EmptyHeartSprite;

                // Shake if sprite changed to an empty heart (i.e., lost a heart)
                if (previousSprite != _heartImages[i].sprite && !shouldBeFull)
                {
                    StartCoroutine(ShakeHeart(_heartImages[i]));
                }
            }
            else
            {
                _heartImages[i].style.display = DisplayStyle.None;
            }
        }
    }

    private IEnumerator ShakeHeart(Image heartImage)
    {
        // Shake left
        heartImage.AddToClassList("heart-shake");
        yield return new WaitForSeconds(0.1f);
        
        // Shake right
        heartImage.style.translate = new Translate(-5, 0);
        yield return new WaitForSeconds(0.1f);
        
        // Return to center
        heartImage.RemoveFromClassList("heart-shake");
        heartImage.style.translate = new Translate(0, 0);
    }

    void OnDestroy()
    {
        HUDManager.Instance.OnCoinsUpdated -= OnCoinsUpdated;
        HUDManager.Instance.OnHeartsUpdated -= OnHeartsUpdated;
    }
}
