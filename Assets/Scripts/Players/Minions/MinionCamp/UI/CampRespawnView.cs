using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CampRespawnView : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private Image _fill;
    [SerializeField] private TMP_Text _timeText;

    private int _lastSeconds = -1;

    public void SetIcon(Sprite sprite)
    {
        if (_icon == null) return;
        _icon.sprite = sprite;
        _icon.enabled = sprite != null;
    }

    public void SetProgress(float remaining, float duration)
    {
        if (_fill != null)
            _fill.fillAmount = duration > 0f ? Mathf.Clamp01(remaining / duration) : 0f;

        if (_timeText == null) return;

        int seconds = Mathf.CeilToInt(remaining);
        if (seconds == _lastSeconds) return;
        _lastSeconds = seconds;
        _timeText.text = seconds > 0 ? seconds.ToString() : string.Empty;
    }

    public void Show(Sprite icon, float remaining, float duration)
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        SetIcon(icon);
        SetProgress(remaining, duration);
    }

    public void Hide()
    {
        if (gameObject.activeSelf) gameObject.SetActive(false);
    }
}