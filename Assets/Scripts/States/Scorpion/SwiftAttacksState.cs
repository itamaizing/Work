using System.Collections.Generic;
using UnityEngine;

public class SwiftAttacksState : RefreshingState
{
    private int _attacksLeft = 3;
    private const float SpeedBonusPercent = 1.0f;

    private readonly AttributeModifier _castSpeedModifier = new AttributeModifier(SpeedBonusPercent, ModifierType.Percent);

    private readonly List<Skill> _affectedSkills = new();

    public override States State => States.SwiftAttacks;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => new List<StatusEffect>();

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit,
        Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        duration = durationToExit;
        _attacksLeft = 3;

        _castSpeedModifier.Source = this;

        ApplySpeedBuff();

        _affectedSkills.Clear();
        if (characterState?.Character?.Abilities?.Abilities != null)
        {
            foreach (var skill in characterState.Character.Abilities.Abilities)
            {
                if (skill != null)
                {
                    _affectedSkills.Add(skill);
                    skill.CastEnded += OnAttackPerformed;
                }
            }
        }

        MaxStacksCount = 1;
        currentStacksCount = 1;
    }

    private void ApplySpeedBuff()
    {
        if (characterState == null || characterState.Character == null) return;

        var castSpeedAttr = characterState.Character.AttributeSystem[CharacterAttributeName.CastSpeed];

        if (castSpeedAttr != null && !castSpeedAttr.Modifiers.Contains(_castSpeedModifier))
        {
            castSpeedAttr.AddModifier(_castSpeedModifier);
        }
    }

    private void RemoveSpeedBuff()
    {
        if (characterState == null || characterState.Character == null) return;

        var castSpeedAttr = characterState.Character.AttributeSystem[CharacterAttributeName.CastSpeed];

        if (castSpeedAttr != null)
        {
            castSpeedAttr.RemoveModifier(_castSpeedModifier);
        }
    }

    private void OnAttackPerformed()
    {
        if (_attacksLeft <= 0) return;

        _attacksLeft--;

        if (_attacksLeft <= 0)
        {
            ExitState();
        }
    }

    public override void UpdateState()
    {
        if (duration <= 0)
        {
            ExitState();
        }
    }

    public override void ExitState()
    {
        currentStacksCount = 0;
        
        foreach (var skill in _affectedSkills)
        {
            if (skill != null)
            {
                skill.CastEnded -= OnAttackPerformed;
            }
        }
        _affectedSkills.Clear();

        RemoveSpeedBuff();
        base.ExitState();
        characterState?.RemoveState(this);
    }

    public override bool Stack(float time) => false;
}