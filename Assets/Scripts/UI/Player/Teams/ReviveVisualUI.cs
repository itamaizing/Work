using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReviveVisualUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _text;
    [SerializeField] private Image _image;

    private double _reviveAt;
    private int _displayedSeconds;

    public void StartTimer(float time)
    {
        _reviveAt = Time.realtimeSinceStartupAsDouble + time;
        Refresh();
    }

    private void OnEnable() => Refresh();

    private void Update()
    {
        if (_reviveAt > 0) Refresh();
    }

    private void Refresh()
    {
        var seconds = Mathf.Max(0, Mathf.CeilToInt((float)(_reviveAt - Time.realtimeSinceStartupAsDouble)));
        if (seconds == 0) _reviveAt = 0;
        if (seconds == _displayedSeconds) return;

        _displayedSeconds = seconds;
        _image.gameObject.SetActive(seconds > 0);
        if (seconds > 0) _text.SetText("{0}", seconds);
    }
}
