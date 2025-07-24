using UnityEngine;

public class HuntressTalent_5 : Talent
{
    [SerializeField] private ReconnaissanceFire reconnaissanceFire;
    [SerializeField] private TerrifyingElfAura terrifyingElfAura;

    public override void Enter()
    {
        reconnaissanceFire.partialBlindnessTalentActive(true);
        terrifyingElfAura.ElvenSkillTalent(true);
    }

    public override void Exit()
    {
        reconnaissanceFire.partialBlindnessTalentActive(false);
        terrifyingElfAura.ElvenSkillTalent(false);
    }
}
