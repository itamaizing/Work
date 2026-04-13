using System.Collections.Generic;
using UnityEngine;

public class DisciplineHpAuraTalent : Talent
{
    private readonly List<Skill> _disciplineSkills = new();
    private Character _priest;

    public override void Enter()
    {
        _disciplineSkills.Clear();
        _priest = character.GetComponent<Character>();

        foreach (var skill in character.Abilities.Abilities)
        {
            if (skill.Info.School != Schools.Discipline) continue;
            skill.CastEnded -= OnDisciplineSkillCast;
            skill.CastEnded += OnDisciplineSkillCast;
            _disciplineSkills.Add(skill);
        }
    }

    public override void Exit()
    {
        foreach (var skill in _disciplineSkills)
            skill.CastEnded -= OnDisciplineSkillCast;

        _disciplineSkills.Clear();
        _priest = null;
    }

    private void OnDisciplineSkillCast()
    {
        if (_priest == null) return;

        _priest.CharacterState.CmdAddState(States.DisciplineAura, 6f, 0f, _priest.gameObject, nameof(DisciplineHpAuraTalent));
    }
}
