using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoisonTalent_2 : Talent
{
    [SerializeField] private PoisonBall poisonBall;
    [SerializeField] private PoisonSlap poisonSlap;
    [SerializeField] private SpitPoison spitPoison;
    [SerializeField] private SkillManager skillManager;

    public override void Enter()
    {
        skillManager.ActivateSkill(poisonBall);
        poisonBall.SetPoisonCloudEnabled(true);
        poisonSlap.SetPoisonCloudEnabled(true);
        spitPoison.SetPoisonCloudEnabled(true);
    }

    public override void Exit()
    {
        skillManager.DeactivateSkill(poisonBall);
        poisonBall.SetPoisonCloudEnabled(false);
        poisonSlap.SetPoisonCloudEnabled(false);
        spitPoison.SetPoisonCloudEnabled(false);
    }
}
