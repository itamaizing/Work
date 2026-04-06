using UnityEngine;

public class DomeProcTalent : Talent
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
