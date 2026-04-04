using UnityEngine;

public class ReptileTalent_3 : Talent
{
    [SerializeField] private CreeperPoisonAura _creeperPoisonAura;

    public override void Enter()
    {
        _creeperPoisonAura.ActiveWitheringPoison(true);
    }

    public override void Exit()
    {
        _creeperPoisonAura.ActiveWitheringPoison(false);
    }
}
