using UnityEngine;

public class ShieldAttackTalent : Talent
{
    [SerializeField] private PriestShield _priestShield;
    
    public override void Enter()
    {
       // _priestShield.EnableShieldAttackTalent(true);
    }

    public override void Exit()
    {
        //_priestShield.EnableShieldAttackTalent(false);
    }
}
