using UnityEngine;

public class ElementalsActiveTalent : Talent
{
    public override void Enter()
    {
        character.Abilities.GetSkill<ElementalSpawn>()?.IsElementalsActiveSkillsEnabled(true);
    }

    public override void Exit()
    {
        character.Abilities.GetSkill<ElementalSpawn>()?.IsElementalsActiveSkillsEnabled(false);
    }
}
