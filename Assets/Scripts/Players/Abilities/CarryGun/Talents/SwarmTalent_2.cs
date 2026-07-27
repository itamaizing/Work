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
        skillManager.GetSkill<Tentacles>().ActivateWombSpawning(true);
        AddingDescriptionSet(true);
    }

    public override void Exit()
    {
        skillManager.DeactivateSkill(swarmCapacity);
        skillManager.DeactivateSkill(_wombSpawn);
        skillManager.GetSkill<Tentacles>().ActivateWombSpawning(false);

        AddingDescriptionSet(false);
    }

    private void AddingDescriptionSet(bool value)
    {
        swarmCapacity.AddingDescriptionSet(value, Data.DescriptionsForInfoPanel[1]);
        _wombSpawn.AddingDescriptionSet(value, Data.DescriptionsForInfoPanel[0]);
    }
}
