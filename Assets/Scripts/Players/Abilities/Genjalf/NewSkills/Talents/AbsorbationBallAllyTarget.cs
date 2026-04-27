using UnityEngine;

public class AbsorbationBallAllyTarget : Talent
{
    [SerializeField] private Gangdollarff.AbsorptionBall _absorptionBall;
    
    public override void Enter()
    {
        _absorptionBall.IsAllyTargetAvailable = true;
    }

    public override void Exit()
    {
        _absorptionBall.IsAllyTargetAvailable = false;
    }
}
