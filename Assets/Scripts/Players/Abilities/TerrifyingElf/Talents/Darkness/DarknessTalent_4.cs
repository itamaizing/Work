using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarknessTalent_4 : Talent
{
    [SerializeField] private Ghost ghost;
    [SerializeField] private SkillManager skillManager;

    public override void Enter()
    {
        character.Abilities.ActivateSkill(character.Abilities.GetSkill<Ghost>());
    }

    public override void Exit()
    {
        character.Abilities.DeactivateSkill(character.Abilities.GetSkill<Ghost>());
    }
}
