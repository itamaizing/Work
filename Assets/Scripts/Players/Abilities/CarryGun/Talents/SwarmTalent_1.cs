using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwarmTalent_1 : Talent
{
    [SerializeField] private SkillManager skillManager;
    [SerializeField] private Tentacles tentacles;

    public override void Enter()
    {
        skillManager.ActivateSkill(tentacles);
        AddingDescriptionSet(true);
    }

    public override void Exit()
    {
        skillManager.DeactivateSkill(tentacles);
        AddingDescriptionSet(false);
    }

    private void AddingDescriptionSet(bool value)
    {
        tentacles.AddingDescriptionSet(value, Data.DescriptionsForInfoPanel[0]);
    }
}
