using UnityEngine;

public class FireMagicChargeTalent : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<FireMagicActivationBooster>().Enable(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<FireMagicActivationBooster>().Enable(false);
    }
}