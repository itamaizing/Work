using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarknessTalent_7 : Talent
{
    [SerializeField] private SubjugationMind subjugationMind;
    [SerializeField] private SkillManager ability;

    public override void Enter()
    {
        //ability.ActivateSkill(subjugationMind); #ПЕРЕНЕСТИ В 13М
        character.Abilities.ActivateSkill(character.Abilities.GetSkill<Suppression>());

    }

    public override void Exit()
    {
        //ability.DeactivateSkill(subjugationMind); #ПЕРЕНЕСТИ В 13М
        character.Abilities.DeactivateSkill(character.Abilities.GetSkill<Suppression>());
    }
}
