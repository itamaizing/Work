using UnityEngine;

public class PsionicsTalent_4 : Talent
{
    [SerializeField] private Tentacles _tentacles;
    [SerializeField] private PsionicEnergySkill _psionicEnergySkill;

    public override void Enter()
    {
        _psionicEnergySkill.DischargingPsiTalen(true);
        _tentacles.ProtectiveCooconSpawnAttack(true);
        _tentacles.PsionicsTalentThree(true);
    }

    public override void Exit()
    {
        _psionicEnergySkill.DischargingPsiTalen(false);
        _tentacles.ProtectiveCooconSpawnAttack(false);
        _tentacles.PsionicsTalentThree(false);
    }
}
