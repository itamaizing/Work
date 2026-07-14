using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrostTalent_1 : Talent
{
    [SerializeField] private IcyStream _icyStream;
    [SerializeField] private SkillManager _skillManager;

    public override void Enter()
    {
        _skillManager.ActivateSkill(_icyStream);
    }

    public override void Exit()
    {
        _skillManager.DeactivateSkill(_icyStream);
    }
}
