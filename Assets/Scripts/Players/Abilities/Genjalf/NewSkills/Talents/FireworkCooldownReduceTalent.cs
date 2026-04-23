using Gangdollarff;
using UnityEngine;

public class FireworkCooldownReduceTalent : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<FireworkDsplay>().SetCooldownReduceTalent(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<FireworkDsplay>().SetCooldownReduceTalent(false);
    }
}
