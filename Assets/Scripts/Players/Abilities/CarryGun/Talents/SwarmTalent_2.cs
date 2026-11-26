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
        SetDescriptionSupplement(true);
    }

    public override void Exit()
    {
        skillManager.DeactivateSkill(swarmCapacity);
        skillManager.DeactivateSkill(_wombSpawn);
        skillManager.GetSkill<Tentacles>().ActivateWombSpawning(false);

        SetDescriptionSupplement(false);
    }

    protected override void UpdateDescription() => SetDescriptionSupplement(Data.IsOpen);

    private void SetDescriptionSupplement(bool value)
    {
        swarmCapacity.SetDescriptionSupplement(this, value, Data.DescriptionTable.FindEntry("description.1"), this);
        _wombSpawn.SetDescriptionSupplement(this, value, Data.DescriptionTable.FindEntry("description.0"), this);
    }
}
