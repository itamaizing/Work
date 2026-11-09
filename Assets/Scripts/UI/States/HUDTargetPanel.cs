using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class HUDTargetPanel : MonoBehaviour
{
    [SerializeField] private GameObject _targetInfo;
    [SerializeField] private Image _portrait;
    [SerializeField] private ResourceBar[] _bars;

    private Character _target;
    private CanvasGroup _visibility;

    private void Awake()
    {
        _visibility = GetComponent<CanvasGroup>();
        SetVisible(false);
    }

    private void LateUpdate()
    {
        var selector = TargetSelector.Instance;
        var target = selector != null ? selector.InspectedTarget : null;
        SetTarget(target != null && target.IsInitialized && !target.IsDead ? target : null);

        foreach (var bar in _bars)
            bar.Refresh();
    }

    private void OnDisable() => SetTarget(null);

    private void SetTarget(Character target)
    {
        if (ReferenceEquals(_target, target)) return;

        _target = target;
        _portrait.sprite = target != null && target.Data != null ? target.Data.Icon : null;
        _portrait.enabled = _portrait.sprite != null;

        foreach (var bar in _bars)
        {
            bar.Bind(target);
            bar.Root.transform.SetAsLastSibling();
        }

        if (target != null && target.Resource != null && target.Resource != target.Health)
        {
            foreach (var bar in _bars)
            {
                if (bar.Type != target.Resource.Type) continue;

                bar.Root.transform.SetSiblingIndex(1);
                break;
            }
        }

        SetVisible(target != null);
    }

    private void SetVisible(bool visible)
    {
        _visibility.alpha = visible ? 1f : 0f;
        _visibility.blocksRaycasts = visible;
        _targetInfo.SetActive(visible);
    }

    [Serializable]
    private sealed class ResourceBar
    {
        [SerializeField] private ResourceType _type;
        [SerializeField] private GameObject _root;
        [SerializeField] private RectTransform _fill;
        [SerializeField] private TMP_Text _label;

        private Resource _resource;
        private float _current = float.NaN;
        private float _max = float.NaN;

        public ResourceType Type => _type;
        public GameObject Root => _root;

        public void Bind(Character target)
        {
            _resource = null;
            if (target != null)
            {
                target.TryGetResource(_type, out _resource);
                if (_type == ResourceType.Health && target.Health != null)
                    _resource = target.Health;
            }

            _current = float.NaN;
            _max = float.NaN;
            _root.SetActive(_resource != null);
        }

        public void Refresh()
        {
            if (_resource == null) return;

            float current = _resource.CurrentValue;
            float max = _resource.MaxValue;
            if (current == _current && max == _max) return;

            _current = current;
            _max = max;
            _fill.anchorMax = new Vector2(max > 0f ? Mathf.Clamp01(current / max) : 0f, 1f);
            _label.SetText("{0:0} / {1:0}", current, max);
        }
    }
}
