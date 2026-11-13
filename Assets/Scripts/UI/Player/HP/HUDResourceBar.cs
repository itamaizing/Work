using System.Collections.Generic;
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
    [SerializeField] private Rune _runePrefab;
    [SerializeField] private Resource _initialResource;

    private readonly List<Rune> _runes = new();
    private Resource _resource;
    private float _current = float.NaN;
    private float _max = float.NaN;
    private float _regenPerSecond = float.NaN;
    private Vector2 _runeTrackSize;
    private HorizontalLayoutGroup _runeLayout;
    private float _runeAspect;

    public ResourceType Type => _type;

    public Resource FindResource(Character character)
    {
        if (character == null) return null;
        if (character.TryGetResource(Type, out var resource)) return resource;

        return Type == ResourceType.AttackingPsi ? character.GetComponent<AttackingPsionicEnergy>() : null;
    }

    private void Start()
    {
        if (_initialResource != null && _resource == null)
            Bind(_initialResource);
    }

    public void Bind(Resource resource)
    {
        _resource = resource;
        gameObject.SetActive(_resource != null);
        if (_resource == null) return;

        Image fillColor = _fill.GetComponent<Image>();
        if (Type == ResourceType.Rune && _runePrefab != null)
        {
            fillColor.enabled = false;
            if (_amount != null) _amount.enabled = false;
            if (_regeneration != null) _regeneration.enabled = false;
            _fill.anchorMax = Vector2.one;
            _runeLayout = _fill.GetComponent<HorizontalLayoutGroup>();
            
            Sprite sprite = _runePrefab.GetComponent<Image>().sprite;
            _runeAspect = sprite.rect.width / sprite.rect.height;
        }
        else
        {
            fillColor.color = G.Colors.Gameplay.Resources[Type].Bar ?? fillColor.color;
        }

        Refresh();
    }

    private void LateUpdate() => Refresh();

    private void Refresh()
    {
        if (_resource == null) return;

        float current = _resource.CurrentValue;
        float max = _resource.MaxValue;
        bool showRunes = Type == ResourceType.Rune && _runePrefab != null;
        Vector2 trackSize = showRunes ? _fill.rect.size : Vector2.zero;
        if (current != _current || max != _max || trackSize != _runeTrackSize)
        {
            _current = current;
            _max = max;
            _runeTrackSize = trackSize;
            if (showRunes)
            {
                RefreshRunes(current, max);
            }
            else
            {
                _fill.anchorMax = new Vector2(max > 0f ? Mathf.Clamp01(current / max) : 0f, 1f);
                _amount.SetText("{0:0}/<b>{1:0}</b>", current, max);
            }
        }

        if (showRunes) return;

        float rate = _resource.RegenerationPerSecond;
        if (rate == _regenPerSecond) return;

        _regenPerSecond = rate;
        _regeneration.text = rate.ToString("+0.##;-0.##;0", CultureInfo.InvariantCulture);
    }

    private void RefreshRunes(float current, float max)
    {
        current = Mathf.Clamp(current, 0f, Mathf.Max(0f, max));
        int visibleRunes = Mathf.CeilToInt(current);
        int capacity = Mathf.Max(1, Mathf.CeilToInt(max));

        const float minGapRatio = 0.15f;
        const float compactGapRatio = 0.4f;

        float trackWidth = Mathf.Max(0f, _runeTrackSize.x);
        float iconWidth = Mathf.Min(Mathf.Max(0f, _runeTrackSize.y) * _runeAspect, trackWidth / (capacity + (capacity - 1) * minGapRatio));

        int gapsAtCapacity = Mathf.Max(1, capacity - 1);
        float fullGap = Mathf.Max(0f, trackWidth - iconWidth * capacity) / gapsAtCapacity;
        float compactGap = Mathf.Min(iconWidth * compactGapRatio, fullGap);
        float occupancy = Mathf.Clamp01((visibleRunes - 1f) / gapsAtCapacity);

        _runeLayout.spacing = Mathf.Lerp(compactGap, fullGap, occupancy * occupancy);

        while (_runes.Count < visibleRunes)
        {
            Rune rune = Instantiate(_runePrefab, _fill, false);
            Image image = rune.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            _runes.Add(rune);
        }

        for (int i = 0; i < _runes.Count; i++)
        {
            Rune rune = _runes[i];
            bool visible = i < visibleRunes;
            rune.gameObject.SetActive(visible);
            if (!visible) continue;

            rune.GetComponent<LayoutElement>().preferredWidth = iconWidth;
            rune.SetValue(Mathf.Clamp01(current - i));
        }
    }
}
