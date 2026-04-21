using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrostTalent_2 : Talent
{
    [SerializeField] private IcePuddle _icePuddle;
    [SerializeField] private SkillManager _skillManager;

    public override void Enter()
    {
        _skillManager.ActivateSkill(_icePuddle);
    }

    public override void Exit()
    {
        _skillManager.DeactivateSkill(_icePuddle);
    }
}
