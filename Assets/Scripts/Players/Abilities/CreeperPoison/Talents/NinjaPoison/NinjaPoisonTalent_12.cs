using UnityEngine;

public class NinjaPoisonTalent_12 : Talent
{
    [SerializeField] private CreeperInvisible _creeperInvisible;

    public override void Enter()
    {
        _creeperInvisible.InvisibilitStrike(true);
    }

    public override void Exit()
    {
        _creeperInvisible.InvisibilitStrike(false);
    }
}