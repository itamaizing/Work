using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReleaseFromSecrecy : Talent
{
    [SerializeField] private CreeperInvisible _creeperInvisible;
    [SerializeField] private CreeperStrike _creeperStrike;

    private int _maxCountBuff = 1;
    private int _currentCountBuff = 0;

    private float _startTimeDurationBuff = 1.0f;
    private float _timeDurationBuff;

    private float _attackSpeedIncrease = 0.5f;

    public override void Enter()
    {
        SetActive(true);
        _timeDurationBuff = _startTimeDurationBuff;
    }

    public override void Exit()
    {
        SetActive(false);
        _timeDurationBuff = 0;
    }

    public void ApplyBuff()
    {
        if (!_creeperInvisible.IsInvisible)
        {
            if (_currentCountBuff < _maxCountBuff)
            {
                _currentCountBuff++;
                IncreaseAttackSpeed();

                Invoke("ReturnOriginalAttackSpeed", _timeDurationBuff);
            }
        }
    }

    private void IncreaseAttackSpeed()
    {
        _creeperStrike.Buff.AttackSpeed.IncreasePercentage(_attackSpeedIncrease);
        Debug.Log($"ReleaseFromSecrecy / ReturnOriginalAttackSpeed / _creeperStrike.Buff.AttackSpeed.Increase = {_creeperStrike.Buff.AttackSpeed.Multiplier}");
    }

    private void ReturnOriginalAttackSpeed()
    {
        _creeperStrike.Buff.AttackSpeed.ReductionPercentage(_attackSpeedIncrease);
        Debug.Log($"ReleaseFromSecrecy / ReturnOriginalAttackSpeed / _creeperStrike.Buff.AttackSpeed.Reduction = {_creeperStrike.Buff.AttackSpeed.Multiplier}");
        _timeDurationBuff = _startTimeDurationBuff;
        _currentCountBuff = 0;
    }

}
