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
        _waveSkill.SetSizeModifier(_waveBonusSize);
        _lightningStrike.EnableChain(true);
    }

    public override void Exit()
    {
        _quicksand.SetQuickSandInvisible(false);
        _waveSkill.RemoveSizeModifier();
        _lightningStrike.EnableChain(false); 
    }
}
