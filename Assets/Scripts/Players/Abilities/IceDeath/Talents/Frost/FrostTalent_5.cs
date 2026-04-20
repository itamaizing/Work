using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrostTalent_5 : Talent
{
    [SerializeField] private IceCloud _iceCloud;
    [SerializeField] private SkillManager _skillManager;

    public override void Enter()
    {
        _skillManager.ActivateSkill(_iceCloud);
    }

    public override void Exit()
    {
        _skillManager.DeactivateSkill(_iceCloud);
    }
}
