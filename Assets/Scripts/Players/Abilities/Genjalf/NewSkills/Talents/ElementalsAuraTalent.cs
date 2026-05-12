using UnityEngine;

public class ElementalsAuraTalent : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<ElementalSpawn>()?.IsElementalsAuraEnabled(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<ElementalSpawn>()?.IsElementalsAuraEnabled(false);
    }
}
