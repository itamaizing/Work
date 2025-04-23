using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IcePuddleActiveTalent : Talent
{
    [SerializeField] private IcePuddle icePuddle;
    [SerializeField] private SkillManager skillManager;

    public override void Enter()
    {
        skillManager.ActivateSkill(icePuddle);
    }

    public override void Exit()
    {
        skillManager.DeactivateSkill(icePuddle);
    }
}
