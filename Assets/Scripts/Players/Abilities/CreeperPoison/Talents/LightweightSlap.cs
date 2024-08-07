using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightweightSlap : Talent
{
    [SerializeField] private CreeperStrike _creeperStrike;

    private const float _timeBetweenAttack = 0.1f;

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
        }
    }
}
