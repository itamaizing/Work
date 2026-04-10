using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReptileTalent_9 : Talent
{
    [SerializeField] private ReflectiveScales _reflectiveScales;
    [SerializeField] private SkillManager _skillManager;


    public override void Enter()
    {
        _skillManager.ActivateSkill(_reflectiveScales);
    }

    public override void Exit()
    {
        _skillManager.DeactivateSkill(_reflectiveScales);
    }
}

