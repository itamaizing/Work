using UnityEngine;

public class QuicksandInvisibleTalent : Talent
{
    [SerializeField] private Quicksand _quicksand;
    [Space]
    [SerializeField] private WaveSkill _waveSkill;
    [SerializeField] private Vector2 _waveBonusSize;
    [Space]
    [SerializeField] private LightningStrike _lightningStrike;

    public override void Enter()
    {
        _quicksand.SetQuickSandInvisible(true);
        _waveSkill.SetBonusSize(_waveBonusSize);
        _lightningStrike.EnableChain(true);
        //_quicksand.SetBonusSize(2, 1f);
    }

    public override void Exit()
    {
        _quicksand.SetQuickSandInvisible(false);
        _waveSkill.SetBonusSize(Vector2.zero);
        _lightningStrike.EnableChain(false);
        //_quicksand.SetBonusSize(0, 0f);
    }
}
