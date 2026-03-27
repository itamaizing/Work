using UnityEngine;

public class NinjaPoisonTalent_8 : Talent
{
    [SerializeField] private SpitPoison _spitPoison;
    [SerializeField] private PoisonBall _poison;

    public override void Enter()
    {
        _spitPoison.TransparentPoisons(true);
        _poison.TransparentPoisons(true);

    }

    public override void Exit()
    {
        _spitPoison.TransparentPoisons(false);
        _poison.TransparentPoisons(false);
    }
}