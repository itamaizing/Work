using System.Collections.Generic;
using UnityEngine;

public class OtherForceStateStacking : RefreshingStateStacking
{
    private float _currentBonus = 0f;

    private Resource _healthResource;
    private float _originalCurrentPercent;

    public override States State => States.OtherForces;

    public override StateType Type => StateType.Magic;
    public override Schools Schools => Schools.Dark;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => new();

    private float _baseDuration;

    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        _baseDuration = durationToExit;
        RemainingDuration = durationToExit;

        _healthResource = character.Character.TryGetResource(ResourceType.Health);

        if (_healthResource == null)
            ExitState();

        _originalCurrentPercent = _healthResource.CurrentValue / _healthResource.MaxValue;

        _currentBonus = ParseHealthBonus(skillName);

        if (_currentBonus > 0f && !characterState.isClient)
        {
            _healthResource.AddMax(_currentBonus, keepPercent: true);
        }

        SetMaxStacks(1);
        currentStacksCount = 1;
    }

    public override void UpdateState()
    {
        if (RemainingDuration <= 0)
        {
            ExitState();
        }
    }

    public override void ExitState()
    {
        if (_healthResource != null && _currentBonus > 0f && !characterState.isClient)
        {
            _healthResource.AddMax(-_currentBonus, keepPercent: false);

            float currentPercent =
                _healthResource.CurrentValue / _healthResource.MaxValue;

            float restoredPercent =
                Mathf.Min(currentPercent, _originalCurrentPercent);

            _healthResource.InstCurrentValue(
                restoredPercent * _healthResource.MaxValue);
        }

        _currentBonus = 0f;
        currentStacksCount = 0;

        characterState?.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        RemainingDuration = _baseDuration;
        return true;
    }

    
    
    private void AddBonus(float value)
    {
        if (_healthResource == null || value <= 0f)
            return;

        _currentBonus += value;

        if (!characterState.isClient)
        {
            _healthResource.AddMax(value, keepPercent: true);
        }
    }

    private float ParseHealthBonus(string skillName)
    {
        if (string.IsNullOrEmpty(skillName))
            return 0f;

        int separatorIndex = skillName.LastIndexOf(':');

        if (separatorIndex < 0)
            return 0f;

        string valuePart = skillName.Substring(separatorIndex + 1);

        return float.TryParse(valuePart, out float result) ? result : 0f;
    }
}
