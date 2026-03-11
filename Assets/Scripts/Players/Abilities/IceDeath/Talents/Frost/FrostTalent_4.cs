using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrostTalent_4 : Talent
{
    [SerializeField] private IcePuddle _icePuddle;

    public override void Enter()
    {
        _icePuddle.IceDeathInIcePudleTalentActive(true);
        AddingDescriptionSet(true);
    }

    public override void Exit()
    {
        _icePuddle.IceDeathInIcePudleTalentActive(false);
        AddingDescriptionSet(false);
    }

    private void AddingDescriptionSet(bool value)
    {
        _icePuddle.AddingDescriptionSet(value, Data.DescriptionsForInfoPanel[0]);
    }    
}
