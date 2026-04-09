using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NatureTalent_10 : Talent
{
    [SerializeField] private ShotAstral shotAstral;
    [SerializeField] private SkillManager ability;

    public override void Enter()
    {
        ability.ActivateSkill(shotAstral);
    }

    public override void Exit()
    {
        ability.DeactivateSkill(shotAstral);
    }
}
