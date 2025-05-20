using UnityEngine;

public class HuntressTalent_5 : Talent
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
