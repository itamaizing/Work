using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinjaPoisonTalent_1 : Talent
{
    [SerializeField] private FocusingOnReflexes focusingOnReflexes;
    [SerializeField] private SkillManager skillManager;

    public override void Enter()
    {
        skillManager.ActivateSkill(focusingOnReflexes);
    }

    public override void Exit()
    {
        skillManager.DeactivateSkill(focusingOnReflexes);
    }
}
