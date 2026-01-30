using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CampSurrenderUI : MonoBehaviour
{
    [SerializeField] private RectTransform _panel;
    [SerializeField] private Button _option1Button;
    [SerializeField] private Button _option2Button;

    [Header("Animation")]
    [SerializeField] private float _showDuration = 0.3f;
    [SerializeField] private float _hideDuration = 0.25f;
    [SerializeField] private Ease _showEase = Ease.OutBack;
    [SerializeField] private Ease _hideEase = Ease.InBack;

    private MinionCamp _camp;
    private Tween _currentTween;

    private void Awake()
    {
        if (_panel != null)
        {
            _panel.localScale = Vector3.zero;
        }
    }

    private void Start()
    {
        if (_option1Button != null)
        {
            _option1Button.onClick.AddListener(OnOption1);
        }

        if (_option2Button != null)
        {
            _option2Button.onClick.AddListener(OnOption2);
        }
    }

    public void Setup(MinionCamp camp)
    {
        if(camp != null) return;
        _camp = camp;
    }

    public void Show()
    {
        if (_panel == null)
            return;

        _currentTween?.Kill();

        gameObject.SetActive(true);

        _panel.localScale = Vector3.zero;
        _currentTween = _panel
            .DOScale(Vector3.one, _showDuration)
            .SetEase(_showEase);
    }

    public void Hide()
    {
        if (_panel == null)
        {
            Destroy(gameObject);
            return;
        }

        _currentTween?.Kill();

        _currentTween = _panel
            .DOScale(Vector3.zero, _hideDuration)
            .SetEase(_hideEase)
            .OnComplete(() =>
            {
                Destroy(gameObject);
            });
    }

    private void OnOption1()
    {
        if (_camp != null)
        {
            _camp.CmdChooseOption1();
        }

        Hide();
    }

    private void OnOption2()
    {
        if (_camp != null)
        {
            _camp.CmdChooseOption2();
        }

        Hide();
    }

    private void OnDestroy()
    {
        _currentTween?.Kill();
    }
}
