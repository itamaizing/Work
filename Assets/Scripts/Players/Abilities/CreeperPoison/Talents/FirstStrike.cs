using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstStrike : Talent
{
    [SerializeField] private CreeperInvisible _creeperInvisible;
    [SerializeField] private CreeperStrike _creeperStrike;

    private bool _isCanIncreaseCrit = false;

    public bool FirstHit = false;
    public bool IsCanIncreaseCrit { get => _isCanIncreaseCrit; set => _isCanIncreaseCrit = value; }
    public override void Enter()
    {
        SetActive(true);
    }

    public override void Exit()
    {
        SetActive(false);
    }

    public bool SetBoolTrue()
    {
        _isCanIncreaseCrit = true;
        FirstHit = true;

        return _isCanIncreaseCrit && FirstHit;
    }

    public bool ReturnBoolFalse()
    {
        if (_isCanIncreaseCrit)
        {
            _isCanIncreaseCrit = false;
            FirstHit = false;
        }
        return _isCanIncreaseCrit;
    }
}
