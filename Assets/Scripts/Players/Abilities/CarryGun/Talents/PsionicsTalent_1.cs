using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PsionicsTalent_1 : Talent
{
    [SerializeField] private SkillManager skillManager;
    [SerializeField] private BasePsionicEnergy basePsionicEnergy;
    [SerializeField] private Conversion conversion;

    public override void Enter()
    {
        skillManager.ActivateSkill(conversion);
        basePsionicEnergy.PsionicsTalentOne(true);
    }

    public override void Exit()
    {
        skillManager.DeactivateSkill(conversion);
        basePsionicEnergy.PsionicsTalentOne(false);
    }
}
