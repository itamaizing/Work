using UnityEngine;

public class AoeSparkTalent : Talent
{
    [SerializeField] private SparkOfLight _spark;
    [SerializeField] private FlowOfLight _flowOfLight;

    public override void Enter()
    {
        _spark.SetAoeTalent(true);
        _flowOfLight.SetAoeTalent(true);
    }

    public override void Exit()
    {
        _spark.SetAoeTalent(false);
        _flowOfLight.SetAoeTalent(false);
    }
}
