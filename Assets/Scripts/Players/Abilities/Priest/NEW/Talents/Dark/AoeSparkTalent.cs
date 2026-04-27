using UnityEngine;

public class AoeSparkTalent : Talent
{
    public override void Enter()
    {
        var flow = character.Abilities.GetSkill<FlowOfLight>();
        var spark = character.Abilities.GetSkill<SparkOfLight>();

        flow?.AoeBooster.Enable(true);
        spark?.AoeBooster.Enable(true);
    }

    public override void Exit()
    {
        var flow = character.Abilities.GetSkill<FlowOfLight>();
        var spark = character.Abilities.GetSkill<SparkOfLight>();

        flow?.AoeBooster.Enable(false);
        spark?.AoeBooster.Enable(false);
    }
}
