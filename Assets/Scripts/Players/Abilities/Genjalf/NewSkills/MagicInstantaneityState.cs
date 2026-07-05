using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class MagicInstantaneityState : StackableState
{
    private List<StatusEffect> _effects = new();
    private List<Skill> _buffedSkills = new();
    private Character _character;
    private float _time;
    private float _percent = 0.20f;

    public override States State => States.MagicInstantaneity;

    public override StateType Type => StateType.Magic;

    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;

    public override List<StatusEffect> Effects => _effects;

    protected override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        //CanStack = true;
        _time = durationToExit;
        _character = character.Character;
        MaxStacksCount = 5;
        currentStacksCount = 1;

        var skillsWithDelay = _character.Abilities.Abilities
            .Where(s => s.CastDeley > 0 && s.IsSkillActive)
            .ToList();

        _buffedSkills = skillsWithDelay;

        foreach (var skill in _buffedSkills)
            skill.Buff.CastSpeed.IncreasePercentage(1 - (_percent * CurrentStacksCount)); ;
    }

    protected override void ExitState()
    {
        foreach (var skill in _buffedSkills)
            skill.Buff.CastSpeed.Reset();
        _buffedSkills.Clear();
        _character.CharacterState.RemoveStateFromList(this);
    }

    public override bool Stack(float time)
    {
        if (CurrentStacksCount < MaxStacksCount)
        {
            currentStacksCount++;
            foreach (var skill in _buffedSkills)
            {
                skill.Buff.CastSpeed.Reset();
                skill.Buff.CastSpeed.IncreasePercentage(1 - (_percent * CurrentStacksCount));
            }
        }
        _time = time;
        return true;
    }

    public override void UpdateState()
    {
    }
}
