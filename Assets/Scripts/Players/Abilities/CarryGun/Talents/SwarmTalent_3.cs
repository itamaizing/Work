using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwarmTalent_3 : Talent
{
    [SerializeField] private Tentacles _tentacles;

    public override void Enter()
    {
        _tentacles.InjectionAdrenaline(true);
        _tentacles.AttractionTentacleTalent(true);
        AddingDescriptionSet(true);
    }

    public override void Exit()
    {
        _tentacles.InjectionAdrenaline(false);
        _tentacles.AttractionTentacleTalent(false);
        AddingDescriptionSet(false);
    }

    private void AddingDescriptionSet(bool value)
    {
        _tentacles.AddingDescriptionSet(value, Data.DescriptionsForInfoPanel[0]);
    }
}
