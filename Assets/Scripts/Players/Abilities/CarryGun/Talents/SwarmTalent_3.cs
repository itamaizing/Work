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
        AddingDescriptionSet(true);
    }

    public override void Exit()
    {
        _swarmCapacity.IsAddCharges = false;
        _tentacles.AttractionTentacleTalent(false);
        AddingDescriptionSet(false);
    }

    private void AddingDescriptionSet(bool value)
    {
        _tentacles.AddingDescriptionSet(value, Data.DescriptionsForInfoPanel[0]);
    }
}
