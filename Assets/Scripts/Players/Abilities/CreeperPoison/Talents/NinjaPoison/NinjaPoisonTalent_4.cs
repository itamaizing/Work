using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaPoisonTalent_4 : Talent
{
    [SerializeField] private ColdBlood coldBlood;

    [SerializeField] private SkillManager skillManager;

    public override void Enter()
    {
        skillManager.ActivateSkill(coldBlood);
    }

    public override void Exit()
    {
        skillManager.DeactivateSkill(coldBlood);
    }
}