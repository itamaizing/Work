using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaTalent_1 : Talent
{
    [SerializeField] private IceRolling _iceRolling;
    [SerializeField] private SkillManager _manager;

    public override void Enter()
    {
        _manager.ActivateSkill(_iceRolling);
        _iceRolling.RollingWithEnemyTalentActive(true, Data.Level);
    }

    public override void Exit()
    {
        _manager.DeactivateSkill(_iceRolling);
        _iceRolling.RollingWithEnemyTalentActive(false, 0);
    }
}
