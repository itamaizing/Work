using UnityEngine;

public class AbsorbedSwordDamageTalent : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<AbsorbationSwordSkill>().EnableAbsorbedDamage(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<AbsorbationSwordSkill>().EnableAbsorbedDamage(false);
    }
}
