using UnityEngine;

public class DarkTalent_7 : Talent
{
    [SerializeField] private SparkOfLight _spark;
    [SerializeField] private FlowOfLight _flowOfLight;

    [SerializeField] private float _baffDuration;
    [SerializeField] private float _baffAdditionalTime;
    [Range(0.0f, 1.0f)]
    [SerializeField] private float _baffChance;

    public override void Enter()
    {
        _spark.DestructionFillingTalent(true, _baffDuration, _baffAdditionalTime, _baffChance);
        _flowOfLight.DestructionFillingTalent(true, _baffDuration, _baffAdditionalTime, _baffChance);
    }

    public override void Exit()
    {
        _spark.DestructionFillingTalent(false, _baffDuration, _baffAdditionalTime, _baffChance);
        _flowOfLight.DestructionFillingTalent(false, _baffDuration, _baffAdditionalTime, _baffChance);
    }
}
