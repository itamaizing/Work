using UnityEngine;

public class HuntressTalent_5 : Talent
{
    [SerializeField] private TerrifyingElfAura terrifyingElfAura;
    [SerializeField] private GrowTree growTree;

    public override void Enter()
    {
        terrifyingElfAura.CalmnessOnElvenSkillTalent(true);
        growTree.GrowTreeArrowIntoSkyRadius(true);
    }

    public override void Exit()
    {
        terrifyingElfAura.CalmnessOnElvenSkillTalent(false);
        growTree.GrowTreeArrowIntoSkyRadius(false);
    }
}
