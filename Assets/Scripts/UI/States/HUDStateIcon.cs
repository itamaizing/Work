using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HUDStateIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image _icon;
    [SerializeField] private Image _border;
    [SerializeField] private Image _fade;
    [SerializeField] private TextMeshProUGUI _text;

    internal StateBasic State { get; private set; }

    internal void Bind(StateBasic state, StateIcoData data, Color color)
    {
        State = state;

        if (data?.Icon != null) _icon.sprite = data.Icon;
        if (state.DisplayIcon != null) _icon.sprite = state.DisplayIcon;
        if (_icon.sprite == null) _icon.color = new Color32(30, 30, 30, 255);
        _border.color = data != null && data.BorderColor != Color.white ? data.BorderColor : color;
        _text.color = color;

        if (isActiveAndEnabled) SubscribeToState();
    }

    private void OnEnable()
    {
        if (State != null) SubscribeToState();
    }

    private void SubscribeToState()
    {
        State.OnDurationChanged += UpdateDuration;
        State.OnTextChanged += UpdateText;
        UpdateDuration(State, State.RemainingDuration, State.MaxDuration);
        UpdateText(State, State.DisplayText);
    }

    private void OnDisable()
    {
        if (State == null) return;

        State.OnDurationChanged -= UpdateDuration;
        State.OnTextChanged -= UpdateText;
        StateTooltip.Instance?.Hide(this);
    }

    private void UpdateDuration(StateBasic state, float current, float max)
    {
        _fade.fillAmount = max > 0f ? Mathf.Clamp01(1f - current / max) : 0f;
    }

    private void UpdateText(StateBasic state, string text)
    {
        _text.text = text;
        _text.gameObject.SetActive(!string.IsNullOrEmpty(text));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        StateTooltip.Instance?.RequestShow(this, State);
    }

    public void OnPointerExit(PointerEventData eventData) => StateTooltip.Instance?.Hide(this);
}
