using UnityEngine;

public class SpiritHealthOnDestruction : Talent
{
    [SerializeField] private ShadowSkill _shadow;
    [SerializeField] private RisingOfShadows _risingOfShadows;
    [SerializeField] private Restoration _restoration;
    [SerializeField] private FlowOfLight _flowOfLight;
    [SerializeField] private SparkOfLight _spark;
    public override void Enter()
    {
        _shadow.EnableSpiritHealth(true);
        _risingOfShadows.EnableSpiritHealth(true);
        _restoration.EnableSpiritHealth(true);
        _flowOfLight.EnableSpiritHealth(true);
        _spark.EnableSpiritHealth(true);
    }

    public override void Exit()
    {
        _shadow.EnableSpiritHealth(false);
        _risingOfShadows.EnableSpiritHealth(false);
        _restoration.EnableSpiritHealth(false);
        _flowOfLight.EnableSpiritHealth(false);
        _spark.EnableSpiritHealth(false);
    }
}
