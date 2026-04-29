using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathTalent_1 : Talent
{
    [SerializeField] private SeriesOfStrikes _seriesOfStrikes;

    public override void Enter()
    {
        _seriesOfStrikes.IceRuneTalentActive(true);
    }

    public override void Exit()
    {
        _seriesOfStrikes.IceRuneTalentActive(false);
    }
}
