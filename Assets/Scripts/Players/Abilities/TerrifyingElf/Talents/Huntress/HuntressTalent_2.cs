using UnityEngine;

public class HuntressTalent_2 : Talent
{
    [SerializeField] private ReconnaissanceFire reconnaissanceFire;
    [SerializeField] private TerrifyingElfAura terrifyingElfAura;

    public override void Enter()
    {
        reconnaissanceFire.FireWorshipperTalentActive(true);
        terrifyingElfAura.FireWorshipperTalentActive(true);
        reconnaissanceFire.ReconnaissanceFireAuraDarknesActive(true);
    }

    public override void Exit()
    {
        reconnaissanceFire.FireWorshipperTalentActive(false);
        terrifyingElfAura.FireWorshipperTalentActive(false);
        reconnaissanceFire.ReconnaissanceFireAuraDarknesActive(false);
    }
}
