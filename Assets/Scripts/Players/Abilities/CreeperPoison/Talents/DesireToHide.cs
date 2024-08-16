using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DesireToHide : Talent
{
    [SerializeField] private CreeperStrike _creeperStrike;
    [SerializeField] private CreeperInvisible _creeperInvisible;

    private float _timeBeforeApplyInvisible = 10.0f;
    private float _timeForApplicationInvisible;
    private float _startTimeForApplicationInvisible = 2.0f;

    public bool IsCanApply = false;

    public override void Enter()
    {
        SetActive(true);
        _timeForApplicationInvisible = _startTimeForApplicationInvisible;
    }

    public override void Exit()
    {
        SetActive(false);
    }

    private void Update()
    {
        _timeBeforeApplyInvisible -= Time.deltaTime;
        if (_timeBeforeApplyInvisible <= 0)
        {
            if (IsCanApply)
            {
                _timeForApplicationInvisible -= Time.deltaTime;
                if (_timeForApplicationInvisible <= 0)
                {
                    IsCanApply = false;
                    _timeForApplicationInvisible = _startTimeForApplicationInvisible;
                }

            }
        }
    }

    public bool IsCanApplyInvisible()
    {
        IsCanApply = true;

        return IsCanApply;
    }
}

