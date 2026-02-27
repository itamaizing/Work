using UnityEngine;

public class QuicksandInvisibleTalent : Talent
{
    [SerializeField] private Quicksand _quicksand;

    public override void Enter()
    {
        _quicksand.SetQuickSandInvisible(true);
        _quicksand.SetBonusSize(2, 1f);
    }

    public override void Exit()
    {
        _quicksand.SetQuickSandInvisible(false);
        _quicksand.SetBonusSize(0, 0f);
    }
}
