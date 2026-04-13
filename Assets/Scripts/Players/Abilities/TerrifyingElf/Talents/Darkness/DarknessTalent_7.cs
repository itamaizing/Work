using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarknessTalent_7 : Talent
{
    [SerializeField] private SubjugationMind subjugationMind;
    [SerializeField] private SkillManager ability;
    [SerializeField] private RetributiveReckoning retributiveReckoning;

    public override void Enter()
    {
        ability.ActivateSkill(subjugationMind);
        ability.ActivateSkill(retributiveReckoning);
    }

    public override void Exit()
    {
        ability.DeactivateSkill(subjugationMind);
        ability.DeactivateSkill(retributiveReckoning);
    }
}
