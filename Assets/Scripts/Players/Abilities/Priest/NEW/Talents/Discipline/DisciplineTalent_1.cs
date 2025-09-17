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
        ability.ActivateSkill(priestShield);
        ability.ActivateSkill(emeraldSkin);
        priestShield.EnableDisciplineShieldBoost(true);
    }

    public override void Exit()
    {
        ability.DeactivateSkill(priestShield);
        ability.DeactivateSkill(emeraldSkin);
        priestShield.EnableDisciplineShieldBoost(false);
    }
}
