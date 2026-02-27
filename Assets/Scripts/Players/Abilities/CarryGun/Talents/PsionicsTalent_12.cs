using UnityEngine;

public class PsionicsTalent_12 : Talent
{
    [SerializeField] private PsionicEnergySkill _psionicEnergySkill;

    public override void Enter()
    {
        _psionicEnergySkill.ExtendedDuration(true);
    }

    public override void Exit()
    {
        _psionicEnergySkill.ExtendedDuration(false);
    }
}