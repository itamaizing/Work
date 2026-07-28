using UnityEngine;

public class SwarmTalent_5 : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<Tentacles>().ExtendDurationOnDamageTalent(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<Tentacles>().ExtendDurationOnDamageTalent(true);
    }
}
