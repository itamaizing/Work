using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillIcon : MonoBehaviour, IDropHandler
{
    [SerializeField] private Image _boxFrame;
    [SerializeField] private TMP_Text _key;

    [SerializeField] private FillAmountOverTime _cooldown;
    [SerializeField] private TextMeshProUGUI _chargeCounter;

    private int _index;
    private FillAmountOverTime _castLine;
    private DraggableIcon _currentIcon;

    public event Action<int, Skill> CurrentSkillChenged;

    public DraggableIcon CurrentIcon 
    {
        get => _currentIcon;
        set
        {
            if (value == null)
            {
                if(_currentIcon != null)
                    UnsubscribingSkillOnEvents(_currentIcon.Skill);

                Deselected();

                _currentIcon = value;

                CurrentSkillChenged?.Invoke(_index, null);
                return;
            }
            else if (_currentIcon == null)
            {
                _currentIcon = value;

                if (_currentIcon.Selected)
                    Selected();
                else
                    Deselected();

                SubscribingSkillOnEvents(_currentIcon.Skill);
            }
            else
            {
                UnsubscribingSkillOnEvents(_currentIcon.Skill);

                _currentIcon = value;

                if (_currentIcon.Selected)
                    Selected();
                else
                    Deselected();

                SubscribingSkillOnEvents(_currentIcon.Skill);
            }
            CurrentSkillChenged?.Invoke(_index, _currentIcon.Skill);
        }
    }

    public TMP_Text Key { get => _key; }
    public FillAmountOverTime CastLine { get => _castLine; set => _castLine = value; }
    public int Index { get => _index; }

    public void Init(int index, FillAmountOverTime castLine)
    {
        _index = index;
        _castLine = castLine;
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject dropped = eventData.pointerDrag;
        DraggableIcon draggableIcon = dropped.GetComponent<DraggableIcon>();

        if(CurrentIcon == null)    
        {
            draggableIcon.PatentAfterDrag = transform;
            CurrentIcon = draggableIcon;
        }
        else 
        {
            CurrentIcon.PatentAfterDrag = draggableIcon.PatentAfterDrag;
            CurrentIcon.OnEndDrag(null);
            draggableIcon.PatentAfterDrag = transform;
            CurrentIcon = draggableIcon;
        }
    }

    public void OnStartCooldown(float dutarion)
    {
        _cooldown.StartFill(dutarion, 1, 0, false);
        _cooldown.gameObject.SetActive(true);
    }

    public void Selected()
    {
        if (_currentIcon != null)
        {
            _boxFrame.color = Color.green;
            _currentIcon.Selected = true;
        }
    }

    public void Deselected()
    {
        _boxFrame.color = Color.white;

        if (_currentIcon != null)
            _currentIcon.Selected = false;
    }

    private void SubscribingSkillOnEvents(Skill ability)
    {
        ability.CastStreamStarted += OnStartStreaming;
        ability.Canceled += OnStopStreaming;

        ability.CastDeleyStarted += OnStartCastDeley;
        ability.Canceled += OnStopCastDeley;

        ability.CooldownStarted += OnStartCooldown;
        ability.CurrentChargeChanged += OnCurrentChargeText;
    }

    private void UnsubscribingSkillOnEvents(Skill ability)
    {
        ability.CastStreamStarted -= OnStartStreaming;
        ability.Canceled -= OnStopStreaming;

        ability.CastDeleyStarted -= OnStartCastDeley;
        ability.Canceled -= OnStopCastDeley;

        ability.CooldownStarted -= OnStartCooldown;
        ability.CurrentChargeChanged -= OnCurrentChargeText;
    }

    private void OnCurrentChargeText(int value)
    {
        if (value > 0)
            _chargeCounter.color = Color.green;
        else
            _chargeCounter.color = Color.red;

        _chargeCounter.text = value.ToString();
    }

    private void OnStartStreaming(float time)
    {
        _castLine.gameObject.SetActive(true);
        _castLine.StartFill(time, 1, 0);
    }

    private void OnStopStreaming()
    {
        _castLine.gameObject.SetActive(false);
        _castLine.Stop();
    }

    private void OnStartCastDeley(float time)
    {
        _castLine.gameObject.SetActive(true);
        _castLine.StartFill(time);
    }

    private void OnStopCastDeley()
    {
        _castLine.gameObject.SetActive(false);
        _castLine.Stop();
    }
}
