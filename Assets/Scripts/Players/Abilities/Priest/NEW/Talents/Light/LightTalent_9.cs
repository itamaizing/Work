using UnityEngine;

public class LightTalent_9 : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<DomeOfLight>()?.DomeProcBooster?.Enable(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<DomeOfLight>()?.DomeProcBooster?.Enable(false);
    }
}
