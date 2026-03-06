using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EvolutionTalent_4 : Talent
{
    [SerializeField] private JumpBack jumpBack;

    [SerializeField] private SkillManager skillManager;

    public override void Enter()
    {
        skillManager.ActivateSkill(jumpBack);
    }

    public override void Exit()
    {
        skillManager.DeactivateSkill(jumpBack);
    }
}
