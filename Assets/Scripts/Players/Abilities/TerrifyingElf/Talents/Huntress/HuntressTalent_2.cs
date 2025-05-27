using UnityEngine;

public class HuntressTalent_2 : Talent
{
    [SerializeField] private ReconnaissanceFire reconnaissanceFire;
    [SerializeField] private TerrifyingElfAura terrifyingElfAura;

    public override void Enter()
    {
        reconnaissanceFire.FireWorshipperTalentActive(true);
        reconnaissanceFire.ReconnaissanceFireAuraDarknesActive(true);

        terrifyingElfAura.FireWorshipperTalentActive(true);
        terrifyingElfAura.CalmnessOnElvenSkillTalent(true);
    }

    public override void Exit()
    {
        reconnaissanceFire.FireWorshipperTalentActive(false);
        reconnaissanceFire.ReconnaissanceFireAuraDarknesActive(false);

        terrifyingElfAura.FireWorshipperTalentActive(false);
        terrifyingElfAura.CalmnessOnElvenSkillTalent(false);
    }
}
