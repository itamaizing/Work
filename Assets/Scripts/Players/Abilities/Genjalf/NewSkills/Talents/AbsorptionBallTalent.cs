using UnityEngine;

public class AbsorptionBallTalent : Talent
{
    [SerializeField] private Gangdollarff.AbsorptionBall _absorbationBall;
    [SerializeField] private SkillManager _ability;
    
    public override void Enter()
    {
        _ability.ActivateSkill(_absorbationBall);
    }

    public override void Exit()
    {
        _ability.DeactivateSkill(_absorbationBall);
    }
}
