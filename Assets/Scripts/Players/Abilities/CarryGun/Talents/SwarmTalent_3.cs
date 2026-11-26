using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwarmTalent_3 : Talent
{
    [SerializeField] private Tentacles _tentacles;
    [SerializeField] private SwarmCapacity _swarmCapacity;

    public override void Enter()
    {
        _swarmCapacity.IsAddCharges = true;
        _tentacles.AttractionTentacleTalent(true);
        SetDescriptionSupplement(true);
    }

    public override void Exit()
    {
        _swarmCapacity.IsAddCharges = false;
        _tentacles.AttractionTentacleTalent(false);
        SetDescriptionSupplement(false);
    }

    protected override void UpdateDescription() => SetDescriptionSupplement(Data.IsOpen);

    private void SetDescriptionSupplement(bool value)
    {
        _tentacles.SetDescriptionSupplement(this, value, Data.DescriptionTable.FindEntry("description.0"), this);
    }
}
