using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PsionicsTalent_3 : Talent
{
    [SerializeField] private Impatica _impatica;
    [SerializeField] SkillManager _skillManager;

    public override void Enter()
    {
        _skillManager.ActivateSkill(_impatica);
    }

    public override void Exit()
    {
        _skillManager.DeactivateSkill(_impatica);
    }
}
