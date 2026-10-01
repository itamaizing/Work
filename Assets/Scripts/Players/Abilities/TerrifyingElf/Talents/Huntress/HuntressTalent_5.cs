using UnityEngine;

public class HuntressTalent_5 : Talent
{
    [SerializeField] private TerrifyingElfAura terrifyingElfAura;
    [SerializeField] private GrowTree growTree;

    public override void Enter()
    {
        character.Abilities.ActivateSkill(character.Abilities.GetSkill<ElvenReflexes>());
        //terrifyingElfAura.CalmnessOnElvenSkillTalent(true);
        //growTree.GrowTreeArrowIntoSkyRadius(true);
    }

    public override void Exit()
    {
        character.Abilities.DeactivateSkill(character.Abilities.GetSkill<ElvenReflexes>());
        //terrifyingElfAura.CalmnessOnElvenSkillTalent(false);
        //growTree.GrowTreeArrowIntoSkyRadius(false);
    }
}
