using UnityEngine;
using UnityEngine.Serialization;

public class DarkTalent_7 : Talent
{
    [SerializeField] private SparkOfLight spark;
    [SerializeField] private FlowOfLight flowOfLight;

    [SerializeField] private float baffDuration;
    [SerializeField] private float baffAdditionalTime;
    [Range(0.0f, 1.0f)]
    [SerializeField] private float baffChance;
    
    public override void Enter()
    {
        spark.DestructionFillingTalent(true,baffDuration,baffAdditionalTime,baffChance);
        flowOfLight.DestructionFillingTalent(true,baffDuration,baffAdditionalTime,baffChance);
    }

    public override void Exit()
    {
        spark.DestructionFillingTalent(false,baffDuration,baffAdditionalTime,baffChance);
        flowOfLight.DestructionFillingTalent(false,baffDuration,baffAdditionalTime,baffChance);
    }
}
