using UnityEngine;

public class PartialBlindnessTalent : Talent
{
    [SerializeField] private ReconnaissanceFire reconnaissanceFire;

    public override void Enter()
    {
        reconnaissanceFire.partialBlindnessTalentActive(true);
    }

    public override void Exit()
    {
        reconnaissanceFire.partialBlindnessTalentActive(false);
    }
}
