using UnityEngine;

public class StrokesOfAspiration : Talent
{
    [SerializeField] private CreeperStrike _creeperStrike;

    [SerializeField] private PoisonBall _poisonBall;
    [SerializeField] private SpitPoison _spitPoison;

    private const float _timeBetweenAttack = 0.1f;
    private const float _decreaseCooldownTime = 3.3f;
    public override void Enter()
    {
        SetActive(true);
        if (_creeperStrike.Buff.AttackSpeed.Multiplier > _timeBetweenAttack)
        {
            _creeperStrike.Buff.AttackSpeed.IncreasePercentage(_timeBetweenAttack);
        }
    }

    public override void Exit()
    {
        SetActive(false);
        if (_creeperStrike.Buff.AttackSpeed.Multiplier < 1.0f)
        {
            _creeperStrike.Buff.AttackSpeed.ReductionPercentage(_timeBetweenAttack);
            //Debug.Log("StrokeOfAspiration / Reduction AttackSpeed = " + _creeperStrike.Buff.AttackSpeed.Multiplier);
        }
    }
}
