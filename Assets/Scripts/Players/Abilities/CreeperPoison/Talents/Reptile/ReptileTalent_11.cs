using UnityEngine;

public class ReptileTalent_11 : Talent
{
    [SerializeField] private CreeperPoisonAura _creeperPoisonAura;

    public override void Enter()
    {
        _creeperPoisonAura.DecreaseCooldownDamage(true);
    }

    public override void Exit()
    {
        _creeperPoisonAura.DecreaseCooldownDamage(false);
    }
}

