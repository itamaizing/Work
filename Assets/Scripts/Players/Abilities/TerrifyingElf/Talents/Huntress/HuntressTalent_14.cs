using UnityEngine;

public class HuntressTalent_14 : Talent
{
    [SerializeField] private GroundTrap groundTrap;
    [SerializeField] private ReconnaissanceFire reconnaissanceFire;

    public override void Enter()
    {
        groundTrap.GroundTrapHealthActiveTalent(true);
        reconnaissanceFire.ReconnaissanceFireHealthTalentActive(true);
    }

    public override void Exit()
    {
        groundTrap.GroundTrapHealthActiveTalent(false);
        reconnaissanceFire.ReconnaissanceFireHealthTalentActive(false);
    }
}
