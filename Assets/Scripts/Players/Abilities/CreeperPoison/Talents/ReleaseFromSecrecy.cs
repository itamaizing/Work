using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReleaseFromSecrecy : Talent
{
   // [SerializeField] private Test_AttackSpeedChangedSystem _attackSpeedChangedSystem;
    [SerializeField] private CreeperInvisible _creeperInvisible;
    [SerializeField] private CreeperStrike _creeperStrike;

    private int _maxCountBuff = 1;
    private int _currentCountBuff = 0;

    private float _startTimeDurationBuff = 2f;
    private float _timeDurationBuff;

    private float _currentAttackSpeed;
    private float _attackSpeedIncrease = 0.1f;

    private bool _isCanIncreaseAttackSpeed = true;
    private bool _isIncreasedAttackSpeed = false;

    private Coroutine _increasingAttackSpeedCoroutine;

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

                _increasingAttackSpeedCoroutine = StartCoroutine(IncreasingAttackSpeedJob());
            }
        }
    }

    private IEnumerator IncreasingAttackSpeedJob()
    {
        float baseAttackSpeed = _creeperStrike.Buff.AttackSpeed.Multiplier;

        while (_timeDurationBuff > 0)
        {
            _timeDurationBuff -= Time.deltaTime;

            if (_currentAttackSpeed != _creeperStrike.Buff.AttackSpeed.Multiplier)
            {
                _currentAttackSpeed = _creeperStrike.Buff.AttackSpeed.Multiplier;

                _isCanIncreaseAttackSpeed = true;
                _isIncreasedAttackSpeed = false;
            }

            if (_isCanIncreaseAttackSpeed)
            {
                IncreaseAttackSpeed();

                _isCanIncreaseAttackSpeed = false;
                _isIncreasedAttackSpeed = true;
            }
            yield return null;
        }

        if (_isIncreasedAttackSpeed || _currentAttackSpeed != baseAttackSpeed)
        {
            Debug.Log("if IsIncrease || curAtckSpd != bseAtckSpd");
            
            ReturnOriginalAttackSpeed();
        }

        StopCoroutine(_increasingAttackSpeedCoroutine);
        _increasingAttackSpeedCoroutine = null;
    }

    private void IncreaseAttackSpeed()
    {
        Debug.Log($"ReleaseFromSecrecy / IncreaseAttackSpeed / _creeperStrike.Buff.AttackSpeed = {_creeperStrike.Buff.AttackSpeed.Multiplier}");
        _creeperStrike.Buff.AttackSpeed.IncreasePercentage(_attackSpeedIncrease);
        _currentAttackSpeed = _creeperStrike.Buff.AttackSpeed.Multiplier;
        Debug.Log($"ReleaseFromSecrecy / IncreaseAttackSpeed / _currentAttackSpeed = {_currentAttackSpeed}");
        Debug.Log($"ReleaseFromSecrecy / IncreaseAttackSpeed / _creeperStrike.Buff.AttackSpeed.Increase = {_creeperStrike.Buff.AttackSpeed.Multiplier}");
    }

    private void ReturnOriginalAttackSpeed()
    {
        Debug.Log($"ReleaseFromSecrecy / ReturnOriginalAttackSpeed / _creeperStrike.Buff.AttackSpeed = {_creeperStrike.Buff.AttackSpeed.Multiplier}");

        _currentAttackSpeed = _creeperStrike.Buff.AttackSpeed.Multiplier;
        _creeperStrike.Buff.AttackSpeed.ReductionPercentage(_currentAttackSpeed);

        Debug.Log($"ReleaseFromSecrecy / IncreaseAttackSpeed / _currentAttackSpeed = {_currentAttackSpeed}");
        Debug.Log($"ReleaseFromSecrecy / ReturnOriginalAttackSpeed / _creeperStrike.Buff.AttackSpeed.Reduction = {_creeperStrike.Buff.AttackSpeed.Multiplier}");

        _timeDurationBuff = _startTimeDurationBuff;
        _currentCountBuff = 0;

        _isIncreasedAttackSpeed = false;
    }

}
