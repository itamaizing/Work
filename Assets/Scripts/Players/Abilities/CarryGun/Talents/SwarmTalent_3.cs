using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwarmTalent_3 : Talent
{
    [SerializeField] private Tentacles tentacles;

    public override void Enter()
    {
        tentacles.AttractionTentacleTalent(true);
        AddingDescriptionSet(true);
    }

    public override void Exit()
    {
        tentacles.AttractionTentacleTalent(false);
        AddingDescriptionSet(false);
    }

    private void AddingDescriptionSet(bool value)
    {
        tentacles.AddingDescriptionSet(value, Data.DescriptionsForInfoPanel[2]);
    }
}
