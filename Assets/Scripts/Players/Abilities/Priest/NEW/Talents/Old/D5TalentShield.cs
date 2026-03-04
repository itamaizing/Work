using UnityEngine;

public class D5TalentShield : Talent
{
    [SerializeField] private PriestShield _priestShield;

    public override void Enter()
    {
        _priestShield.EnableTalentPhysicalShieldBoost(true);
    }

    public override void Exit()
    {
        _priestShield.EnableTalentPhysicalShieldBoost(false);
    }
}
