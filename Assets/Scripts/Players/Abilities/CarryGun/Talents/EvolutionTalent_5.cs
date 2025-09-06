using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EvolutionTalent_5 : Talent
{
    [SerializeField] private DeafeningScream deafeningScream;
    [SerializeField] private SkillManager abilities;

    public override void Enter()
    {
        abilities.ActivateSkill(deafeningScream);
    }

    public override void Exit()
    {
        abilities.DeactivateSkill(deafeningScream);
    }
}
