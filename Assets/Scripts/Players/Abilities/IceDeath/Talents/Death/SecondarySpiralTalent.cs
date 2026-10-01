using UnityEngine;

public class SecondarySpiralTalent : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<DeathSpiral>().EnableSecondaryProjectileTalent(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<DeathSpiral>().EnableSecondaryProjectileTalent(false);
    }
}
