using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisciplineTalent_1 : Talent
{
    [SerializeField] private PriestShield priestShield;
    [SerializeField] private EmeraldSkin emeraldSkin;
    [SerializeField] private ReversePolarity reversePolarity;
    [SerializeField] private SkillManager ability;

    public override void Enter()
    {
        priestShield.EnableDisciplineShieldBoost(true);
        ability.ActivateSkill(reversePolarity);
        ability.ActivateSkill(emeraldSkin);
    }

    public override void Exit()
    {
        priestShield.EnableDisciplineShieldBoost(false);
        ability.DeactivateSkill(reversePolarity);
        ability.DeactivateSkill(emeraldSkin);
    }
}
