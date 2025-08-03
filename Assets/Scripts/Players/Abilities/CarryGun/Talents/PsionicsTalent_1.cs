using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PsionicsTalent_1 : Talent
{
    [SerializeField] private SkillManager skillManager;
    [SerializeField] private Conversion conversion;

    public override void Enter()
    {
        skillManager.ActivateSkill(conversion);
    }

    public override void Exit()
    {
        skillManager.DeactivateSkill(conversion);
    }
}
