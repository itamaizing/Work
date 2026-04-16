using UnityEngine;

public class ElementalsShieldTalent : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<ElementalSpawn>()?.IsElementalsShieldsEnabled(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<ElementalSpawn>()?.IsElementalsShieldsEnabled(false);
    }

}
