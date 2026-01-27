using UnityEngine;

public class AbsorbationBallManaRestorationTalent : Talent
{
    [SerializeField] private Gangdollarff.AbsorptionBall _absorptionBall;
    
    public override void Enter()
    {
        _absorptionBall.AbsorbationMultiplier = 2;
    }

    public override void Exit()
    {
        _absorptionBall.AbsorbationMultiplier = 1;
    }
}
