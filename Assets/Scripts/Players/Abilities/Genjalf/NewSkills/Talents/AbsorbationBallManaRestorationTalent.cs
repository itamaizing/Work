using UnityEngine;

public class AbsorbationBallManaRestorationTalent : Talent
{
    [SerializeField] private Gangdollarff.AbsorptionBall _absorptionBall;
    
    public override void Enter()
    {
        _absorptionBall.IsManaRegenActive = true;
    }

    public override void Exit()
    {
        _absorptionBall.IsManaRegenActive = false;
    }
}
