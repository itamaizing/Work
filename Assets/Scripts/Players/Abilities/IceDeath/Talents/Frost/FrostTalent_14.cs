using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrostTalent_14 : Talent
{
    [SerializeField] private IcePuddle _icePuddle;

    public override void Enter()
    {
        _icePuddle.IceDeathInIcePudleTalentActive(true);
    }

    public override void Exit()
    {
        _icePuddle.IceDeathInIcePudleTalentActive(false);
    }

}
