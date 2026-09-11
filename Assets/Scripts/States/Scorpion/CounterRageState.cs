using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class CounterRageStateStacking : RefreshingStateStacking
{
    public float CurrentBonus = 0f;
    private Resource _energyResource;
    private float _originalMaxValue;
    private float _originalCurrentPercent;

    public override States State => States.CounterRage;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => new List<StatusEffect>();

    public override void Apply(CharacterState character, float durationToExit, float damageToExit,
        Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        RemainingDuration = durationToExit;

        _energyResource = character.Character.TryGetResource(ResourceType.Energy);
        if (_energyResource == null) return;

        _originalMaxValue = _energyResource.MaxValue;
        _originalCurrentPercent = _energyResource.CurrentValue / _energyResource.MaxValue;

        _originalMaxValue = _energyResource.MaxValue;

        float maxPossible = _originalMaxValue * 0.50f;
        CurrentBonus = Mathf.Min(damageToExit, maxPossible);

        if (CurrentBonus > 0f)
            _energyResource.AddMax(CurrentBonus, keepPercent: true);

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

    private void AddBonus(float additionalBonus)
    {
        if (_energyResource == null) return;

        float maxPossible = _originalMaxValue * 0.30f;
        float toAdd = Mathf.Min(additionalBonus, maxPossible - CurrentBonus);

        if (toAdd > 0f)
        {
            CurrentBonus += toAdd;
            _energyResource.AddMax(toAdd, keepPercent: false);
        }
    }
    public override void ExitState()
    {
        if (_energyResource != null && CurrentBonus > 0f)
        {
            _energyResource.AddMax(-CurrentBonus, keepPercent: false);

            float currentPercent = _energyResource.CurrentValue / _energyResource.MaxValue;

            float restoredPercent = Mathf.Min(currentPercent, _originalCurrentPercent);
            _energyResource.InstCurrentValue(restoredPercent * _energyResource.MaxValue);
        }

        CurrentBonus = 0f;
        currentStacksCount = 0;
        characterState?.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        RemainingDuration = 3;
        return true;
    }

    
}