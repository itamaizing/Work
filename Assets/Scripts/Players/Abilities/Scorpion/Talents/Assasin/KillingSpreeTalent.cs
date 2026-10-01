using UnityEngine;

public class KillingSpreeTalent : Talent
{
    public override void Enter()
    {
        character.Abilities.ActivateSkill(character.Abilities.GetSkill<KillingSpreePassive>());
    }

    public override void Exit()
    {
        character.Abilities.DeactivateSkill(character.Abilities.GetSkill<KillingSpreePassive>());
    }
}
