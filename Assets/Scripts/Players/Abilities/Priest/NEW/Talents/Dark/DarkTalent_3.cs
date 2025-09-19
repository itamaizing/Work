using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarkTalent_3 : Talent
{
    [SerializeField] private SparkOfLight sparkOfLight;
    [SerializeField] private FlowOfLight flowOfLight;

    public override void Enter()
    {
        sparkOfLight.FillingDestruction(true);
        flowOfLight.FillingDestruction(true);
    }

    public override void Exit()
    {
        sparkOfLight.FillingDestruction(false);
        flowOfLight.FillingDestruction(false);
    }
}
