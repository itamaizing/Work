using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaScorpionTalent_2 : Talent
{
    [SerializeField] private ConsumeCombo_Scorpion consume;
    [SerializeField] SkillManager manager;

    public override void Enter()
    {
        manager.ActivateSkill(consume);
        consume.ConsumeCombo_ScorpionPhysicStateClearTalent(true);
    }

    public override void Exit()
    {
        manager.DeactivateSkill(consume);
        consume.ConsumeCombo_ScorpionPhysicStateClearTalent(false);
    }
}
