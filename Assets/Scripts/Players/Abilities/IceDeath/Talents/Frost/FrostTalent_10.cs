using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrostTalent_10 : Talent
{
    [SerializeField] BlockOfIce _blockOfIce;
    [SerializeField] private SkillManager _skillManager;


    public override void Enter()
    {
        _skillManager.ActivateSkill(_blockOfIce);
    }

    public override void Exit()
    {
        _skillManager.DeactivateSkill(_blockOfIce);
    }
}
