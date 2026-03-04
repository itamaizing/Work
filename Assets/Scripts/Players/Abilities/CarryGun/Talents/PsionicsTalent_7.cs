using System;
using UnityEngine;

public class PsionicsTalent_7 : Talent
{
    [SerializeField] private PsionicGeneration _psionicGeneration;
    [SerializeField] private SkillManager _skillManager;

    public override void Enter()
    {
        _skillManager.ActivateSkill(_psionicGeneration);
    }

    public override void Exit()
    {
        _skillManager.DeactivateSkill(_psionicGeneration);
    }
}
