using Mirror;
using UnityEngine;

public class HuntressTalent_3 : Talent
{
    [SerializeField] private TerrifyingElfAura terrifyingElfAura;
    [SerializeField] private ReconnaissanceFire reconnaissanceFire;

    public override void Enter()
    {
        terrifyingElfAura.HuntressMarkPhysicsTalentActive(true);
        reconnaissanceFire.ReconnaissanceFireHealthTalentActive(true);
    }

    public override void Exit()
    {
        terrifyingElfAura.HuntressMarkPhysicsTalentActive(false);
        reconnaissanceFire.ReconnaissanceFireHealthTalentActive(false);
    }
}
