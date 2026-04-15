using UnityEngine;

public class HotBloodAuraTalent : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<ElementalSpawn>()?.IsHotAuraEnabled(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<ElementalSpawn>()?.IsHotAuraEnabled(false);
    }
}
