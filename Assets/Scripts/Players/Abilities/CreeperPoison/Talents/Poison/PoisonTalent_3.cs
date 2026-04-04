using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoisonTalent_3 : Talent
{
    [SerializeField] private SkillManager skillManager;
    [SerializeField] private PoisonSlap _poisonSlap;

    public override void Enter()
    {
        skillManager.ActivateSkill(_poisonSlap);
    }

    public override void Exit()
    {
        skillManager.DeactivateSkill(_poisonSlap);
    }
}

