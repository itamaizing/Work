using UnityEngine;

public class DarknessTalent_5 : Talent
{
    [SerializeField] private TerrifyingElfAura terrifyingElfAura; 
    public override void Enter()
    {
        terrifyingElfAura.ManaAbsorptionPhysicalTalentActive(true);
    }

    public override void Exit()
    {
        terrifyingElfAura.ManaAbsorptionPhysicalTalentActive(false);
    }
}
