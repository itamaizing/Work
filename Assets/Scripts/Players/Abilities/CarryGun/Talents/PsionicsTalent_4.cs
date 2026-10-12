using UnityEngine;

public class PsionicsTalent_4 : Talent
{
    [SerializeField] private Tentacles _tentacles;
    [SerializeField] private PsionicEnergySkill _psionicEnergySkill;
    [SerializeField] private BasePsionicEnergy _basePsionicEnergy;

    public override void Enter()
    {
        _basePsionicEnergy.DissipatingPsi(true);
        _tentacles.PsionicsTalentFour(true);
    }

    public override void Exit()
    {
        _basePsionicEnergy.DissipatingPsi(false);
        _tentacles.PsionicsTalentFour(false);
    }
}
