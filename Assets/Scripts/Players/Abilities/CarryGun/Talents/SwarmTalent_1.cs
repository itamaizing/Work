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
        SetDescriptionSupplement(true);
    }

    public override void Exit()
    {
        skillManager.DeactivateSkill(tentacles);
        SetDescriptionSupplement(false);
    }

    protected override void UpdateDescription() => SetDescriptionSupplement(Data.IsOpen);

    private void SetDescriptionSupplement(bool value)
    {
        tentacles.SetDescriptionSupplement(this, value, Data.DescriptionTable.FindEntry("description.0"), this);
    }
}
