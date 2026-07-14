using UnityEngine;

public class DarknessTalent_5 : Talent
{
    [SerializeField] private TerrifyingElfAura terrifyingElfAura;
    [SerializeField] private Silence silence;

    public override void Enter()
    {
        silence.SilenceAddAllCharacterWithDeabaffElf(true);
        terrifyingElfAura.ManaAbsorptionPhysicalTalentActive(true);
    }

    public override void Exit()
    {
        silence.SilenceAddAllCharacterWithDeabaffElf(false);
        terrifyingElfAura.ManaAbsorptionPhysicalTalentActive(false);
    }
}
