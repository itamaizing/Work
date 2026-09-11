using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class StateIcons : MonoBehaviour
{
    [SerializeField] private GameObject _spawnPos;
    [SerializeField] private StateIcoItem _template;
    [SerializeField] private StateIcoDatabase _icoDatabase;
    [SerializeField] private Color _baffColor = new(0.23f, 0.9f, 0.23f);
    [SerializeField] private Color _debaffColor = new(0.9f, 0.15f, 0.15f);
    [SerializeField] private Color _neutralColor = Color.gray;

    private Dictionary<States, StateIcoData> _icoDataDictionary;
    private CharacterState characterState;

    private readonly List<StateIcoItem> _activeIcons = new();
    private readonly Dictionary<AbstractCharacterState, StateIcoItem> _iconByState = new();

    private void Awake()
    {
        characterState = GetComponentInParent<CharacterState>();
        _icoDatabase = Resources.Load<StateIcoDatabase>("StateIcoDatabase_Generated");

        _icoDataDictionary = new();
        foreach (var data in _icoDatabase.Entries)
            if (!_icoDataDictionary.ContainsKey(data.State)) _icoDataDictionary.Add(data.State, data);
    }

    public void RegisterState(AbstractCharacterState state)
    {
        if (state.IsHidden || _iconByState.ContainsKey(state)) return;

        var ico = Instantiate(_template, _spawnPos.transform);
        ico.StateInstance = state;
        ico.State = state.State;

        if (_icoDataDictionary.TryGetValue(state.State, out var data))
        {
            if (data.Icon != null) ico.Icon.sprite = data.Icon;
            ico.border.color = data.BorderColor == Color.white ? GetBorderColor(state.State) : data.BorderColor;

            ico.ResolvedTooltipName = string.IsNullOrEmpty(data.TooltipName) ? state.TooltipName : data.TooltipName;
            ico.ResolvedTooltipDescription = string.IsNullOrEmpty(data.TooltipDescription) ? state.TooltipDescription : data.TooltipDescription;
        }
        else
        {
            ico.border.color = GetFallbackColor(state.State);
            ico.ResolvedTooltipName = state.TooltipName;
            ico.ResolvedTooltipDescription = state.TooltipDescription;
        }

        ico.Text.color = GetTextColor(state.State);

        _activeIcons.Add(ico);
        _iconByState.Add(state, ico);
        ico.transform.SetAsLastSibling();

        state.OnDurationChanged += HandleDurationChanged;
        state.OnTextChanged += HandleTextChanged;

        HandleDurationChanged(state, state.RemainingDuration, state.MaxDuration);
        HandleTextChanged(state, state.DisplayText);
    }

    public void UnregisterState(AbstractCharacterState state)
    {
        state.OnDurationChanged -= HandleDurationChanged;
        state.OnTextChanged -= HandleTextChanged;

        if (!_iconByState.TryGetValue(state, out var ico)) return;

        _iconByState.Remove(state);
        _activeIcons.Remove(ico);

        if (ico != null)
        {
            StateTooltip.Instance?.Hide();
            Destroy(ico.gameObject);
        }
    }

    private void HandleDurationChanged(AbstractCharacterState state, float current, float max)
    {
        if (!_iconByState.TryGetValue(state, out var ico)) return;

        ico.FadeFront.fillAmount = max > 0f ? Mathf.Clamp01(1f - current / max) : 0f;
    }

    private void HandleTextChanged(AbstractCharacterState state, string text)
    {
        if (!_iconByState.TryGetValue(state, out var ico)) return;

        ico.Text.text = text;
        ico.Text.gameObject.SetActive(!string.IsNullOrEmpty(text));
    }

    private Color GetBorderColor(States state) => GetStateColor(state);
    private Color GetTextColor(States state) => GetStateColor(state);
    private Color GetFallbackColor(States state) => GetStateColor(state);

    private Color GetStateColor(States state)
    {
        if (characterState == null || !characterState.enumToState.TryGetValue(state, out var stateObj))
            return _neutralColor;

        return stateObj.BaffDebaff switch
        {
            BaffDebaff.Baff => _baffColor,
            BaffDebaff.Debaff => _debaffColor,
            _ => _neutralColor
        };
    }

    public void DeactivateAll()
    {
        foreach (var state in new List<AbstractCharacterState>(_iconByState.Keys))
        {
            state.OnDurationChanged -= HandleDurationChanged;
            state.OnTextChanged -= HandleTextChanged;
        }

        foreach (var ico in _activeIcons) if (ico != null) Destroy(ico.gameObject);
        _activeIcons.Clear();
        _iconByState.Clear();
    }
}