using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwarmTalent_2 : Talent
{
    [SerializeField] private Tentacles tentacles;
    [SerializeField] private SwarmCapacity swarmCapacity;
    [SerializeField] private SkillManager skillManager;

    public override void Enter()
    {
        skillManager.ActivateSkill(swarmCapacity);
        tentacles.CocoonSpawnTalent(true);

        AddingDescriptionSet(true);
    }

    public override void Exit()
    {
        skillManager.DeactivateSkill(swarmCapacity);
        tentacles.CocoonSpawnTalent(false);

        AddingDescriptionSet(false);
    }

    private void AddingDescriptionSet(bool value)
    {
        swarmCapacity.AddingDescriptionSet(value, Data.DescriptionsForInfoPanel[1]);
        tentacles.AddingDescriptionSet(value, Data.DescriptionsForInfoPanel[0]);
    }
}
