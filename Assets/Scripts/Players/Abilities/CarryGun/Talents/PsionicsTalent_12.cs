using UnityEngine;

public class PsionicsTalent_12 : Talent
{
    [SerializeField] private Impatica _impatica;
    [SerializeField] private AttackingPsionicEnergy _attacking;

    public override void Enter()
    {
        _impatica.ExtendedDuration(true);
        _attacking.ExtendedDuration(true);
    }

    public override void Exit()
    {
        _impatica.ExtendedDuration(false);
        _attacking.ExtendedDuration(false);
    }
}