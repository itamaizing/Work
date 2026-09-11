using System.Collections.Generic;
using UnityEngine;

public class WarmingUpStateStacking : RefreshingStateStacking
{
	private const float BonusPerStack = 0.1f;
	private const float RegenBonusPercent = 1.0f;
	private float _savedRegenValue = 0f;
	private const float RegenMultiplier = 2f;
	
	private readonly AttributeModifier _castSpeedModifier = new AttributeModifier(0f, ModifierType.Percent);
	
	private readonly AttributeModifier _incomingHealModifier = new AttributeModifier(0.1f, ModifierType.Percent);
	private readonly AttributeModifier _healthRegenModifier = new AttributeModifier(RegenBonusPercent, ModifierType.Percent);

    private float _baseDuration;
    
	private List<Skill> _affectedSkills = new();
	private SkillManager _skills;
	public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
	public override States State => States.WarmingUpState;
	public override StateType Type => StateType.Physical;
	public override List<StatusEffect> Effects => new List<StatusEffect>() { StatusEffect.Strengthening };

	private float baseDuration;

	    public WarmingUpStateStacking()
    {
        SetMaxStacks(3);
        currentStacksCount = 0;
    }

    public override void Apply(CharacterState character, float durationToExit, float damageToExit,
        Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        _baseDuration = durationToExit;
        currentStacksCount = 1;

        _castSpeedModifier.Source = this;
        _incomingHealModifier.Source = this;
        _healthRegenModifier.Source = this;

        UpdateCastSpeedBonus();

        if (skillName != null && skillName.Contains("HealingIncrease") && characterState.isServer)
        {
            if (health != null)
            {
                health.AddIncomingModifier(_incomingHealModifier);
            }
            
            if (health != null)
            {
                health.AddModifier(ResourceAttributeName.Regen, _healthRegenModifier);
            }
        }
    }

    public override bool Stack(float time)
    {
        RemainingDuration = time;
        if (currentStacksCount < MaxStacksCount)
        {
            currentStacksCount++;
            UpdateCastSpeedBonus();
            return true;
        }

        return false;
    }

    public override void ReduceStack()
    {
        currentStacksCount--;

        if (currentStacksCount <= 0)
        {
            
            ExitState();
        }
        else
        {

            RemainingDuration = _baseDuration;
            UpdateCastSpeedBonus();
        }
    }

    private void UpdateCastSpeedBonus()
    {
        if (characterState == null || characterState.Character == null) return;

        var castSpeedAttr = characterState.Character.AttributeSystem[CharacterAttributeName.CastSpeed];
        if (castSpeedAttr == null) return;

        _castSpeedModifier.Value = BonusPerStack * currentStacksCount;

        if (!castSpeedAttr.Modifiers.Contains(_castSpeedModifier))
        {
            castSpeedAttr.AddModifier(_castSpeedModifier);
        }
    }

    private void RemoveBuffs()
    {
        if (characterState == null || characterState.Character == null) return;

        // Снимаем бонус скорости каста
        var castSpeedAttr = characterState.Character.AttributeSystem[CharacterAttributeName.CastSpeed];
        if (castSpeedAttr != null)
        {
            castSpeedAttr.RemoveModifier(_castSpeedModifier);
        }

        if (health != null)
        {
            health.RemoveModifierBySource(ResourceAttributeName.Regen, _healthRegenModifier);
        }

        if (characterState.isServer && characterState.Character.Health != null)
        {
            characterState.Character.Health.RemoveIncomingModifier(_incomingHealModifier);
        }
    }

    public override void ExitState()
    {
        currentStacksCount = 0;
        RemoveBuffs();
        
        base.ExitState();
        characterState.RemoveState(this);
    }

    public override void UpdateState() { }

    
}
