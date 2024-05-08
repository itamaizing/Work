using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AbilityIcon : MonoBehaviour
{
    [SerializeField] private Image _abilityIcon;
    [SerializeField] private FillAmountOverTime _cooldown;
    [SerializeField] private TextMeshProUGUI _chargeCounter;
    [SerializeField] private GameObject _abilityNameBox;
    [SerializeField] private TextMeshProUGUI _name;
    [SerializeField] private TextMeshProUGUI _description;

    private FillAmountOverTime _castLine;

    public void Init(Ability ability, FillAmountOverTime castLine)
    {
        _abilityIcon.sprite = ability.Icon;
        _name.text = ability.Name;
        _description.text = ability.Description;
        _castLine = castLine;

        if (ability.IsUseCharges)
        {
            ability.CurrentChargeChange += OnCurrentChargeText;
            _chargeCounter.enabled = true;
            OnCurrentChargeText(ability.Chargers);
        }
        if (ability.IsStreaming)
        {
            ability.StartStreaming += OnStartStreaming;
            ability.StopStreaming += OnStopStreaming;
        }
        ability.StartCastDeley += OnStartCastDeley;
        ability.StopCastDeley += OnStopCastDeley;
    }

    public void OnCurrentChargeText(int value)
    {
        if (value > 0)
            _chargeCounter.color = Color.green;
        else
            _chargeCounter.color = Color.red;

        _chargeCounter.text = value.ToString();
    }

    public void StartCooldown(float dutarion)
    {
        _cooldown.StartFill(dutarion);
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

    private void OnMouseEnter()
    {
        _abilityNameBox.SetActive(true);
    }

    private void OnMouseExit()
    {
        _abilityNameBox.SetActive(false);
    }
}
