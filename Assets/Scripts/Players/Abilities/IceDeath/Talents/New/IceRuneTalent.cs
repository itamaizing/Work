using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IceRuneTalent : Talent
{
    [SerializeField] private SeriesOfStrikes runeComponent;
    [SerializeField] private IceDeathPassiveSkill iceDeathPassiveSkill;

    public override void Enter()
    {
        runeComponent.IceRuneTalentActive(true);
        iceDeathPassiveSkill.EnergyToRestore(true);
    }

    public override void Exit()
    {
        runeComponent.IceRuneTalentActive(false);
        iceDeathPassiveSkill.EnergyToRestore(false);
    }
}
