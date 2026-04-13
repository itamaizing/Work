using UnityEngine;

public class InstantFlashOfLightTalent : Talent
{
    private InstantFlashBooster _flowBooster;
    private InstantFlashBooster _sparkBooster;

    public override void Enter()
    {
        var flow = character.Abilities.GetSkill<FlowOfLight>();
        var spark = character.Abilities.GetSkill<SparkOfLight>();

        flow?.InstantFlashBooster?.Enable(true);
        spark?.InstantFlashBooster?.Enable(true);
        
        _flowBooster = flow?.InstantFlashBooster;
        _sparkBooster = spark?.InstantFlashBooster;
    }

    public override void Exit()
    {
        _flowBooster?.Enable(false);
        _sparkBooster?.Enable(false);
    }
}
