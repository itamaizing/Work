using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CooldownEnergy : Resource
{
    [Header("Main Settings")]
    [SerializeField] private Character _player;
    [SerializeField] private Slider ÒooldownEnergySlider;

    [Header("Skills to Track")]
    [SerializeField] private JumpWithChelicera jumpWithChelicera;
    [SerializeField] private CheliceraStrike cheliceraStrike;
    [SerializeField] private DeafeningScream deafeningScream;
    [SerializeField] private DoubleCheliceraStrike doubleCheliceraStrike;
    [SerializeField] private JumpBack jumpBack;

    public event Action<float, Skill> castCooldownEnergySkill;

    private void Awake()
    {
        InitCooldownEnergyFromJumpSkill();
        UpdateSlider();
    }

    private void OnEnable()
    {
        castCooldownEnergySkill += CooldownEnergySliderMinus;
        StartCoroutine(RegenerateCooldownEnergy());
    }

    private void OnDisable()
    {
        castCooldownEnergySkill -= CooldownEnergySliderMinus;
        StopAllCoroutines();
    }

    private void InitCooldownEnergyFromJumpSkill()
    {
        if (jumpWithChelicera == null)
        {
            Debug.LogError("JumpWithChelicera ÌÂ Á‡‰‡Ì ‚ CooldownEnergy");
            return;
        }

        float totalCooldown = jumpWithChelicera.ChargeCooldown * jumpWithChelicera.MaxChargers;

        _maxValue = totalCooldown;
        _currentValue = _maxValue;
    }

    private IEnumerator RegenerateCooldownEnergy()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);

            if (_currentValue < _maxValue)
            {
                float oldValue = _currentValue;
                _currentValue += 1f;

                if (_currentValue > _maxValue) _currentValue = _maxValue;

                UpdateSlider();
                HookValueChanged(oldValue, _currentValue);
            }
        }
    }

    public void CastCooldownEnergySkill(float time, Skill skill)
    {
        castCooldownEnergySkill?.Invoke(time, skill);
    }

    private void CooldownEnergySliderMinus(float time, Skill skill)
    {
        if (_currentValue < time)
        {
            Debug.LogWarning($"[CooldownEnergy] ÕÂ‰ÓÒÚ‡ÚÓ˜ÌÓ ˝ÌÂ„ËË: ÌÛÊÌÓ {time}, ÂÒÚ¸ {_currentValue}");
            return;
        }

        float oldValue = _currentValue;
        _currentValue -= time;
        UpdateSlider();
        HookValueChanged(oldValue, _currentValue);

        List<Skill> skillsToCooldown = new()
        {
            jumpWithChelicera,
            cheliceraStrike,
            deafeningScream,
            doubleCheliceraStrike,
            jumpBack
        };

        foreach (var skillToCooldown in skillsToCooldown) if (skillToCooldown != null && skillToCooldown != skill) skillToCooldown.IncreaseSetCooldown(time);
    }

    private void UpdateSlider()
    {
        if (ÒooldownEnergySlider != null) ÒooldownEnergySlider.value = _currentValue / _maxValue;
    }
}