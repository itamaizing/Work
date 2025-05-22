using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarknessTalent_6 : Talent
{
    [SerializeField] private RetributiveReckoning retributiveReckoning;
    [SerializeField] private PullingHealth pulling;
    [SerializeField] private SkillManager ability;

    public override void Enter()
    {
        ability.ActivateSkill(retributiveReckoning);
        pulling.PullingHealthThroughGhosts(true);
    }

    public override void Exit()
    {
        ability.DeactivateSkill(retributiveReckoning);
        pulling.PullingHealthThroughGhosts(false);
    }
}
