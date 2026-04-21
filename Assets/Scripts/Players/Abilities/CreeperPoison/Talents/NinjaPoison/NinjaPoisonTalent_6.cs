using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class NinjaPoisonTalent_6 : Talent
{
    [SerializeField] private AmbushPoisons _ambushPoisons;
    [SerializeField] private SkillManager _skillManager;

    public override void Enter()
    {
        _skillManager.ActivateSkill(_ambushPoisons);
    }

    public override void Exit()
    {
        _skillManager.DeactivateSkill(_ambushPoisons);
    }
}

