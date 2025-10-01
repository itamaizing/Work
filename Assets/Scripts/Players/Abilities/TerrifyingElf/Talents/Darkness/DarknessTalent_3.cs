using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarknessTalent_3 : Talent
{
    [SerializeField] private Ghost ghost;
    [SerializeField] private SkillManager skillManager;

    public override void Enter()
    {
        skillManager.ActivateSkill(ghost);
    }

    public override void Exit()
    {
        skillManager.DeactivateSkill(ghost);
    }
}
