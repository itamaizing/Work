using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisciplineTalent_3 : Talent
{
    [SerializeField] private SparkOfLight spark;

    public override void Enter()
    {
        spark.SpiritEnergyTalentActive(true);
    }

    public override void Exit()
    {
        spark.SpiritEnergyTalentActive(true);
    }
}
