using UnityEngine;

public class HuntressTalent_2 : Talent
{
    [SerializeField] private ReconnaissanceFire reconnaissanceFire;
    [SerializeField] private TerrifyingElfAura terrifyingElfAura;
    [SerializeField] private GroundTrap groundTrap;

    public override void Enter()
    {
        groundTrap.GroundTrapHealthActiveTalent(true);
        reconnaissanceFire.FireWorshipperTalentActive(true);

        terrifyingElfAura.FireWorshipperTalentActive(true);
        terrifyingElfAura.CalmnessOnElvenSkillTalent(true);
    }

    public override void Exit()
    {
        groundTrap.GroundTrapHealthActiveTalent(false);
        reconnaissanceFire.FireWorshipperTalentActive(false);

        terrifyingElfAura.FireWorshipperTalentActive(false);
        terrifyingElfAura.CalmnessOnElvenSkillTalent(false);
    }
}
