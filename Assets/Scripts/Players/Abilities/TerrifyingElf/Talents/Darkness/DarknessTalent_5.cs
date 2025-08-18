using UnityEngine;

public class DarknessTalent_5 : Talent
{
    [SerializeField] private TerrifyingElfAura terrifyingElfAura;
    [SerializeField] private Silence silence;

    public override void Enter()
    {
        terrifyingElfAura.ManaAbsorptionPhysicalTalentActive(true);
        terrifyingElfAura.SuppressionManaAbsorption(true);
        silence.SilenceEffectGhostCast(true);
    }

    public override void Exit()
    {
        terrifyingElfAura.ManaAbsorptionPhysicalTalentActive(false);
        terrifyingElfAura.SuppressionManaAbsorption(false);
        silence.SilenceEffectGhostCast(false);
    }
}
