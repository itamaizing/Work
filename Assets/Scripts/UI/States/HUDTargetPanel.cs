using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class HUDTargetPanel : MonoBehaviour
{
    [SerializeField] private GameObject _targetInfo;
    [SerializeField] private Image _portrait;
    [SerializeField] private HUDResourceBar[] _bars;

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
            Resource resource = null;
            if (target != null)
            {
                target.TryGetResource(bar.Type, out resource);
                if (bar.Type == ResourceType.Health && target.Health != null)
                    resource = target.Health;
            }

            bar.Bind(resource);
            bar.transform.SetAsLastSibling();
        }

        if (target != null && target.Resource != null && target.Resource != target.Health)
        {
            foreach (var bar in _bars)
            {
                if (bar.Type != target.Resource.Type) continue;

                bar.transform.SetSiblingIndex(1);
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
}
