using System.Collections.Generic;
using UnityEngine;

public class HealingSlime : RefreshingState
{
    public override States State => States.HealingSlime;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    private readonly List<StatusEffect> _effects = new() { StatusEffect.Healing };

    private const float PercentPerStack = 0.01f;
    
    public float NextStackDueTime { get; set; } = -1f;

    private float _timer;
    private float _remaining;
    private bool _infinite;

    private AttributeModifier _maxHealthModifier = new AttributeModifier(0,ModifierType.Percent);
    private AttributeModifier _regenModifier = new AttributeModifier(0,ModifierType.Percent);

    public override float RemainingDuration => _infinite ? 999f : _remaining;

    public HealingSlime()
    {
        MaxStacksCount = 9;
    }

    public void SwitchToFinite()
    {
        _timer = 0f;
        _infinite = false;
        _remaining = Mathf.Clamp(currentStacksCount, 1, 999f);
    }

    public void SwitchToInfinite()
    {
        _infinite = true;
        _timer = 0f;
        duration = 999f;
    }

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character caster, string skillName)
    {
        currentStacksCount = 1;
        characterState = character;

        SwitchToInfinite();
        
        _maxHealthModifier.Source = this;
        _regenModifier.Source = this;

        if (health != null)
        {
            health.AddModifier(ResourceAttributeName.MaxValue, _maxHealthModifier);
            health.AddModifier(ResourceAttributeName.Regen, _regenModifier);
        }
        
        UpdateAttributeValues(currentStacksCount);
    }

    public override void UpdateState()
    {
        if (_infinite) return;

        _timer += Time.deltaTime;
        if (_timer >= 1f)
        {
            _timer = 0f;

            if (currentStacksCount > 0)
            {
                currentStacksCount--;

                characterState.StateIcons.RemoveIconCount();
            }

            UpdateAttributeValues(currentStacksCount);
            
            _remaining -= 1f;
            if (_remaining <= 0f || currentStacksCount <= 0) ExitState();
        }
    }
    
    public override bool Stack(float _)
    {
        if (currentStacksCount < MaxStacksCount)
        {
            currentStacksCount++;

        }
        UpdateAttributeValues(currentStacksCount);

        if (!_infinite) SwitchToInfinite();

        return true;
    }

    private void UpdateAttributeValues(int stacks)
    {
        float newValue = PercentPerStack * stacks;

        _maxHealthModifier.Value = newValue;
        _regenModifier.Value = newValue;
    }

    public override void ExitState()
    {
        currentStacksCount = 0;
        _infinite = false;

        if (health != null && !characterState.isClient)
        {
            health.RemoveModifierBySource(ResourceAttributeName.MaxValue, this);
            health.RemoveModifierBySource(ResourceAttributeName.Regen, this);
        }

        characterState.RemoveState(this);
    }
    
    public override AbstractCharacterState TryApply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        if (!CanEnterState(character)) return null;

        BaseInit(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);

        if (currentStacksCount == 0)
            EnterState(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);
        else
            Stack(duration);

        return this;
    }
}