using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDResourceBar : MonoBehaviour
{
    [SerializeField] private ResourceType _type;
    [SerializeField] private RectTransform _fill;
    [SerializeField] private TMP_Text _amount;
    [SerializeField] private TMP_Text _regeneration;

    private Resource _resource;
    private float _current = float.NaN;
    private float _max = float.NaN;
    private float _regenPerSecond = float.NaN;

    public ResourceType Type => _type;

    public void Bind(Resource resource)
    {
        _resource = resource;
        gameObject.SetActive(_resource != null);
        Image fillColor = _fill.GetComponent<Image>();
        fillColor.color = G.Colors.Gameplay.Resources[Type].Bar ?? fillColor.color;
        Refresh();
    }

    private void LateUpdate() => Refresh();

    private void Refresh()
    {
        if (_resource == null) return;

        float current = _resource.CurrentValue;
        float max = _resource.MaxValue;
        if (current != _current || max != _max)
        {
            _current = current;
            _max = max;
            _fill.anchorMax = new Vector2(max > 0f ? Mathf.Clamp01(current / max) : 0f, 1f);
            _amount.SetText("{0:0}/<b>{1:0}</b>", current, max);
        }

        float rate = _resource.RegenerationPerSecond;
        if (rate == _regenPerSecond) return;

        _regenPerSecond = rate;
        _regeneration.text = rate.ToString("+0.##;-0.##;0", CultureInfo.InvariantCulture);
    }
}
