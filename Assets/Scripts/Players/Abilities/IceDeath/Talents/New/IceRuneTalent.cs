using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IceRuneTalent : Talent
{
    [SerializeField] private RuneComponent runeComponent;

    public override void Enter()
    {
        runeComponent.IceRuneTalentActive(true);
    }

    public override void Exit()
    {
        runeComponent.IceRuneTalentActive(false);
    }
}
