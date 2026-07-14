using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarknessTalent_7 : Talent
{
    [SerializeField] private SubjugationMind subjugationMind;
    [SerializeField] private SkillManager ability;

    public override void Enter()
    {
        ability.ActivateSkill(subjugationMind);

    }

    public override void Exit()
    {
        ability.DeactivateSkill(subjugationMind);
    }
}
