using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUDStatePanel : MonoBehaviour
{
    [Flags]
    public enum Categories
    {
        None = 0,
        Buff = 1,
        Debuff = 2
    }

    public enum TargetSource
    {
        Manual,
        LocalHero,
        WorldTarget
    }

    [SerializeField] private GridLayoutGroup _grid;
    [SerializeField] private HUDStateIcon _iconPrefab;
    [SerializeField] private StateIcoDatabase _iconDatabase;
    [SerializeField] private TargetSource _targetSource;
    [SerializeField] private Categories _categories = Categories.Buff;
    [SerializeField] private Color _buffColor = new(0.23f, 0.9f, 0.23f);
    [SerializeField] private Color _debuffColor = new(0.9f, 0.15f, 0.15f);

    private readonly List<HUDStateIcon> _icons = new();
    private Character _target;
    private CharacterState _observedStates;

    private void OnEnable()
    {
        if (_targetSource != TargetSource.Manual) _target = ResolveTarget();
        ObserveTarget();
    }

    private void OnDisable() => StopObserving();

    private void LateUpdate()
    {
        if (_targetSource != TargetSource.Manual) SetTarget(ResolveTarget());
    }

    public void SetTarget(Character target)
    {
        if (ReferenceEquals(_target, target)) return;

        StopObserving();
        _target = target;

        if (isActiveAndEnabled) ObserveTarget();
    }

    private Character ResolveTarget()
    {
        if (_targetSource == TargetSource.LocalHero) return Character.Local;

        var selector = TargetSelector.Instance;
        var target = selector != null ? selector.InspectedTarget : null;
        return target != null && !target.IsDead ? target : null;
    }

    private void ObserveTarget()
    {
        if (_target == null) return;

        _observedStates = _target.CharacterState;
        if (_observedStates == null) return;

        _observedStates.OnStateAdded += AddIcon;
        _observedStates.OnStateRemoved += RemoveIcon;
        foreach (var state in _observedStates.CurrentStates)
            AddIcon(state);
    }

    private void StopObserving()
    {
        if (_observedStates != null)
        {
            _observedStates.OnStateAdded -= AddIcon;
            _observedStates.OnStateRemoved -= RemoveIcon;
        }
        _observedStates = null;

        foreach (var icon in _icons)
            DestroyIcon(icon);
        _icons.Clear();
    }

    private void AddIcon(StateBasic state)
    {
        var category = state.BaffDebaff switch
        {
            BaffDebaff.Baff => Categories.Buff,
            BaffDebaff.Debaff => Categories.Debuff,
            _ => Categories.None
        };
        if (state.IsHidden || (_categories & category) == 0) return;

        int index = _icons.FindIndex(icon => state.ApplicationOrder < icon.State.ApplicationOrder);
        if (index < 0) index = _icons.Count;

        var data = _iconDatabase != null ? _iconDatabase.Entries.Find(entry => entry.State == state.State) : null;
        var color = category == Categories.Buff ? _buffColor : _debuffColor;
        var newIcon = Instantiate(_iconPrefab, _grid.transform);
        newIcon.Bind(state, data, color);
        newIcon.transform.SetSiblingIndex(index);
        _icons.Insert(index, newIcon);
    }

    private void RemoveIcon(StateBasic state)
    {
        int index = _icons.FindIndex(icon => icon.State == state);
        if (index < 0) return;

        DestroyIcon(_icons[index]);
        _icons.RemoveAt(index);
    }

    private void DestroyIcon(HUDStateIcon icon)
    {
        icon.gameObject.SetActive(false);
        icon.transform.SetAsLastSibling();
        Destroy(icon.gameObject);
    }
}
