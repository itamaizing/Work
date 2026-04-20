using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrostTalent_3 : Talent
{
    [SerializeField] private FrostEnergy _frostEnergy;
    [SerializeField] private SkillManager _skillManager;

    public override void Enter()
    {
        _skillManager.ActivateSkill(_frostEnergy);
    }

    public override void Exit()
    {
        _skillManager.DeactivateSkill(_frostEnergy);
    }
}
