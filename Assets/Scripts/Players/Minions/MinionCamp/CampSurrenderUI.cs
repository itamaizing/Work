using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CampSurrenderUI : MonoBehaviour
{
    [SerializeField] private RectTransform _panel;
    [SerializeField] private Button _takeCreepsButton;
    [SerializeField] private Button _keepLeaderButton;
    [SerializeField] private Button _killButton;

    [Header("Animation")]
    [SerializeField] private float _showDuration = 0.3f;
    [SerializeField] private float _hideDuration = 0.25f;
    [SerializeField] private Ease _showEase = Ease.OutBack;
    [SerializeField] private Ease _hideEase = Ease.InBack;

    private MinionCamp _camp;
    private Tween _currentTween;

    private void Awake()
    {
        if (_panel != null) _panel.localScale = Vector3.zero;

        if (_takeCreepsButton != null) _takeCreepsButton.onClick.AddListener(() => Choose(SurrenderChoice.TakeCreeps));
        if (_keepLeaderButton != null) _keepLeaderButton.onClick.AddListener(() => Choose(SurrenderChoice.KeepLeader));
        if (_killButton != null) _killButton.onClick.AddListener(() => Choose(SurrenderChoice.Kill));

        if (_camp == null) _camp = GetComponentInParent<MinionCamp>();
    }

    public void Setup(MinionCamp camp)
    {
        if (camp != null) _camp = camp;
    }

    public void Show()
    {
        if (_panel == null) return;

        _currentTween?.Kill();
        gameObject.SetActive(true);

        _panel.localScale = Vector3.zero;
        _currentTween = _panel.DOScale(Vector3.one, _showDuration).SetEase(_showEase);
    }

    public void Hide()
    {
        if (_panel == null || !gameObject.activeInHierarchy)
        {
            gameObject.SetActive(false);
            return;
        }

        _currentTween?.Kill();
        _currentTween = _panel
            .DOScale(Vector3.zero, _hideDuration)
            .SetEase(_hideEase)
            .OnComplete(() => gameObject.SetActive(false));
    }

    private void Choose(SurrenderChoice choice)
    {
        if (_camp != null) _camp.CmdChooseSurrender(choice);
        Hide();
    }

    private void OnDestroy()
    {
        _currentTween?.Kill();
    }
}
