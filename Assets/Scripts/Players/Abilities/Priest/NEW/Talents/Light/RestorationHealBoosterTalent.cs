using UnityEngine;

public class RestorationHealBoosterTalent : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<Restoration>()?.CmdEnableRestorationHealBooster(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<Restoration>()?.CmdEnableRestorationHealBooster(false);
    }
}
