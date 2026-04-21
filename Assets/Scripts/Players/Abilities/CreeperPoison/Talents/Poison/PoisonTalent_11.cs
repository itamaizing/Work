using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoisonTalent_11 : Talent
{
    [SerializeField] ExplosionPoisonCloud _explosionCloud;
    [SerializeField] private SkillManager skillManager;

    public override void Enter()
    {
        skillManager.ActivateSkill(_explosionCloud);
    }

    public override void Exit()
    {
        skillManager.DeactivateSkill(_explosionCloud);
    }
}
