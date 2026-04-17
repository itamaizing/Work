using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaTalent_4 : Talent
{
    [SerializeField] private IcePuddle icePuddle;
    [SerializeField] private IceShard _iceShard;
    [SerializeField] private SkillManager _ability;

    public override void Enter()
    {
        _ability.ActivateSkill(_iceShard);
        //icePuddle.IceDeathInIcePudleTalentActive(true, "");
    }

    public override void Exit()
    {
        _ability.DeactivateSkill(_iceShard);
        //icePuddle.IceDeathInIcePudleTalentActive(false, "");
    }
}
