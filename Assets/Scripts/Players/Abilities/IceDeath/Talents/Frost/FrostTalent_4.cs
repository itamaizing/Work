using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrostTalent_4 : Talent
{
    [SerializeField] IceShadow _iceShadow;
    [SerializeField] private SkillManager _skillManager;


    public override void Enter()
    {
        _skillManager.ActivateSkill(_iceShadow);
    }

    public override void Exit()
    {
        _skillManager.DeactivateSkill(_iceShadow);
    }  
}
