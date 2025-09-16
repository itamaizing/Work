using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrostTalent_2 : Talent
{
    [SerializeField] private IcePuddle icePuddle;
    [SerializeField] private SkillManager skillManager;

    public override void Enter()
    {
        skillManager.ActivateSkill(icePuddle);
        icePuddle.IceDeathInIcePudleTalentActive(true, Data.DescriptionsForInfoPanel[1]);
    }

    public override void Exit()
    {
        skillManager.ActivateSkill(icePuddle);
        icePuddle.IceDeathInIcePudleTalentActive(true, Data.DescriptionsForInfoPanel[1]);
    }
}
