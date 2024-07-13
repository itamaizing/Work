using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StrokesOfAspiration : Talent
{
    [SerializeField] private CreeperStrike _creeperStrike;

    private const float _timeBetweenAttack = 0.1f;

    public override void Enter()
    {
        isActive = true;
        if (_creeperStrike.Buff.AttackSpeed.Multiplier > _timeBetweenAttack)
        {
            _creeperStrike.Buff.AttackSpeed.IncreasePercentage(_timeBetweenAttack);
        }
    }

    public override void Exit()
    {
        isActive = false;
        if (_creeperStrike.Buff.AttackSpeed.Multiplier < 1.0f)
        {
            _creeperStrike.Buff.AttackSpeed.ReductionPercentage(_timeBetweenAttack);
        }
    }
}
