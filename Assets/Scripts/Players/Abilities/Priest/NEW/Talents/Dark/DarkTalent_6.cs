using UnityEngine;

public class DarkTalent_6 : Talent
{
    public override void Enter()
    {
        var flow = character.Abilities.GetSkill<FlowOfLight>();
        var spark = character.Abilities.GetSkill<SparkOfLight>();

        flow?.AoeBooster.Enable(true);
        spark?.DarkTalent_6.Enable(true);
    }

    public override void Exit()
    {
        var flow = character.Abilities.GetSkill<FlowOfLight>();
        var spark = character.Abilities.GetSkill<SparkOfLight>();

        flow?.AoeBooster.Enable(false);
        spark?.DarkTalent_6.Enable(false);
    }
}
