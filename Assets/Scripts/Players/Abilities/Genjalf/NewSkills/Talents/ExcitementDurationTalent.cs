using UnityEngine;

public class ExcitementDurationTalent : Talent
{
    [SerializeField] private MagicalExcitementTalent _magicalExcitementTalent;
    [SerializeField] private float _excitementDuration = 9f;

    public override void Enter()
    {
        _magicalExcitementTalent.IncreaseDuration(_excitementDuration);
    }

    public override void Exit()
    {
        _magicalExcitementTalent.SetDefaultDuration();
    }
}
