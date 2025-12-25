using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaScorpionTalent_2 : Talent
{
    [SerializeField] private ConsumeCombo_Scorpion consume;
    [SerializeField] private PassiveCombo_Scorpion passiveCombo_Scorpion;
    [SerializeField] SkillManager manager;

    public override void Enter()
    {
        manager.ActivateSkill(consume);
        passiveCombo_Scorpion.ConsumeComboTalent(true);
        consume.ConsumeCombo_ScorpionPhysicStateClearTalent(true);
    }

    public override void Exit()
    {
        manager.DeactivateSkill(consume);
        passiveCombo_Scorpion.ConsumeComboTalent(false);
        consume.ConsumeCombo_ScorpionPhysicStateClearTalent(false);
    }
}
