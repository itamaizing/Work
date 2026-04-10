using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class CounterRageState : RefreshingState
{
    public float CurrentBonus = 0f;
    private Resource _energyResource;
    private float _originalMaxValue;
    private float _originalCurrentPercent;

    public override States State => States.CounterRage;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => new List<StatusEffect>();

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit,
        Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        duration = durationToExit;

        _energyResource = character.Character.TryGetResource(ResourceType.Energy);
        if (_energyResource == null) return;

        _originalMaxValue = _energyResource.MaxValue;
        _originalCurrentPercent = _energyResource.CurrentValue / _energyResource.MaxValue;

        _originalMaxValue = _energyResource.MaxValue;

        float maxPossible = _originalMaxValue * 0.30f;
        CurrentBonus = Mathf.Min(damageToExit, maxPossible);

        if (CurrentBonus > 0f)
            _energyResource.AddMax(CurrentBonus, keepPercent: true);

        MaxStacksCount = 1;
        currentStacksCount = 1;
    }

    public override void UpdateState()
    {
        if (duration <= 0)
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
        duration = 3;
        return true;
    }

    public override AbstractCharacterState TryApply(CharacterState character, float durationToExit, 
        float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        if (currentStacksCount == 0)
        {
            BaseInit(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);
            EnterState(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);
        }
        else
        {
            AddBonus(damageToExit);
            Stack(durationToExit);
        }
        return this;
    }
}