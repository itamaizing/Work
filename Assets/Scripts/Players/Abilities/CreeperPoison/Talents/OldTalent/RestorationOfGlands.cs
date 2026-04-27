using UnityEngine;

public class RestorationOfGlands : Talent
{
    [SerializeField] private CreeperStrike _creeperStrike;
    [SerializeField] private SpitPoison _spitPoison;
    [SerializeField] private PoisonBall _poisonBall;
    [SerializeField] private PoisonSlap _poisonSlap;

    private float _baseProcentageReduction = 0.1f;

    private bool _isCanTrigger = false;

    public bool IsCanTrigger { get => _isCanTrigger; set => _isCanTrigger = value; }

    public override void Enter()
    {
        SetActive(true);
    }

    public override void Exit()
    {
        SetActive(false);
    }

    public void ReductionCooldown()
    {
        float baseCooldownSpitPoison = _spitPoison.Cooldown.RemainingTime;
        float baseCooldownPoisonBall = _poisonBall.Cooldown.BaseCooldownTime;

        float procentageCooldownTimeSpitPoison = baseCooldownSpitPoison * _baseProcentageReduction;
        Debug.Log("RestorationOfGlands / ReductionCooldownNotServer / procentageCooldownSpit = " + procentageCooldownTimeSpitPoison);
        //float procentageCooldownTimePoisonBall = baseCooldownPoisonBall * _baseProcentageReduction;
        //Debug.Log("RestorationOfGlands / ReductionCooldownNotServer / procentageCooldownPoisonBall = " + procentageCooldownTimePoisonBall);

        float reducingCooldownSpitPoison = _spitPoison.Cooldown.RemainingTime - procentageCooldownTimeSpitPoison;
        Debug.Log("RestorationOfGlands / ReductionCooldownNotServer / reducingCooldownSpitPoison = " + reducingCooldownSpitPoison);
        //float reducingCooldownPoisonBall = _poisonBall.CooldownTime - procentageCooldownTimePoisonBall;
        //Debug.Log("RestorationOfGlands / ReductionCooldownNotServer / reducingCooldownPoisonBall = " + reducingCooldownPoisonBall);

        _spitPoison.Cooldown.SetReduced(reducingCooldownSpitPoison, shouldModify: false);

        //_poisonBall.ReductionSetCooldown(reducingCooldownPoisonBall);
    }
}
