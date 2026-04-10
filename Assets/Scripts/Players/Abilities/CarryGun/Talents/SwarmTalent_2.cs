using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwarmTalent_2 : Talent
{
    [SerializeField] private WombSpawn _wombSpawn;
    [SerializeField] private SwarmCapacity swarmCapacity;
    [SerializeField] private SkillManager skillManager;

    public override void Enter()
    {
        skillManager.ActivateSkill(swarmCapacity);
        skillManager.ActivateSkill(_wombSpawn);

        AddingDescriptionSet(true);
    }

    public override void Exit()
    {
        skillManager.DeactivateSkill(swarmCapacity);
        skillManager.DeactivateSkill(_wombSpawn);

        AddingDescriptionSet(false);
    }

    private void AddingDescriptionSet(bool value)
    {
        swarmCapacity.AddingDescriptionSet(value, Data.DescriptionsForInfoPanel[1]);
        _wombSpawn.AddingDescriptionSet(value, Data.DescriptionsForInfoPanel[0]);
    }
}
