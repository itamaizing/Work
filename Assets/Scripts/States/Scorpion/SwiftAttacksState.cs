using System.Collections.Generic;
using UnityEngine;

public class SwiftAttacksState : RefreshingState
{
    private int _attacksLeft = 3;
    private float _baseMultiplier = 2f;

    public override States State => States.SwiftAttacks;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => new List<StatusEffect>();

    private List<Skill> _affectedSkills = new();

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit,
        Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        duration = durationToExit;
        _attacksLeft = 3;

        foreach (var skill in characterState.Character.Abilities.Abilities)
        {
            _affectedSkills.Add(skill);
            skill.ExtraAnimationSpeedMultiplier *= _baseMultiplier;

            skill.CastEnded += OnAttackPerformed;
        }
        
        MaxStacksCount = 1;
        currentStacksCount = 1;
    }
    
    public void OnAttackPerformed()
    {
        if (_attacksLeft <= 0) return;

        _attacksLeft--;

        if (_attacksLeft <= 0)
            ExitState();
    }

    public override void UpdateState()
    {
        if (duration <= 0)
            ExitState();
    }

    public override void ExitState()
    {
        currentStacksCount = 1;
        if (characterState != null)
        {
            foreach (var skill in _affectedSkills)
            {
                skill.ExtraAnimationSpeedMultiplier = 1;
                skill.CastEnded -= OnAttackPerformed;
            }
        }
        characterState?.RemoveState(this);
    }

    public override bool Stack(float time) => false;
}