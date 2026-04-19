using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrostTalent_1 : Talent
{
    [SerializeField] private IceCloud _iceCloud;
    [SerializeField] private IcyStream _icyStream;
    [SerializeField] private SkillManager _skillManager;

    public override void Enter()
    {
        //skillManager.ActivateSkill(iceCloud);
        _skillManager.ActivateSkill(_icyStream);
    }

    public override void Exit()
    {
        //skillManager.DeactivateSkill(iceCloud);
        _skillManager.DeactivateSkill(_icyStream);
    }
}
