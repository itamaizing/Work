using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarknessTalent_7 : Talent
{
    [SerializeField] private SubjugationMind subjugationMind;
    [SerializeField] private SkillManager _ability;

    public override void Enter()
    {
        _ability.ActivateSkill(subjugationMind);
    }

    public override void Exit()
    {
        _ability.DeactivateSkill(subjugationMind);
    }
}
