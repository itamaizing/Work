using Mirror;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;

public class MetabolismReptile : Skill
{
    [SerializeField] private Character _player;

    [SerializeField] private PoisonBall _poisonBall;
    [SerializeField] private SpitPoison _spitPoison; 
 
    private float _duration = 3f;

    private float _increaseHealthRegen = 2f;
    private float _increaseCastTime = 2f;
    private float _increaseCooldownTime = 2f;

    private bool _isCanCast = true;
    public bool Enabled;

    protected override bool IsCanCast => _isCanCast;

    protected override IEnumerator PrepareJob()
    {
        throw new System.NotImplementedException();
    }

    protected override IEnumerator CastJob()
    {
        _isCanCast = false;
        TryPayCost();
        IncreaseValues();
        yield return new WaitForSeconds(_duration);
        ClearData();
    }

    protected override void ClearData()
    {
        ResetValues();
    }

    private void IncreaseValues()
    {
        //float currentHpRegen = _player.Health.HpRegenerationValue;
        //float increasedHealthRegen = currentHpRegen * _increaseHealthRegen;
        //_player.Health.HpRegenerationValue = increasedHealthRegen;
        //Debug.Log("HpRegen == " + _player.Health.HpRegenerationValue);

        //float newRemainingCooldownForSpitPoison = _spitPoison.Remaining—ooldownTime / _increaseCooldownTime;
        //_spitPoison.ReductionSetCooldown(newRemainingCooldownForSpitPoison);

        //_poisonBall.Buff.CastSpeed.ReductionPercentage(_increaseCastTime);
        //_spitPoison.Buff.CastSpeed.ReductionPercentage(_increaseCastTime);
    }

    private void ResetValues()
    {
        //float currentHpRegen = _player.Health.HpRegenerationValue;
        //float increasedHealthRegen = currentHpRegen / _increaseHealthRegen;
        //_player.Health.HpRegenerationValue = increasedHealthRegen;
        //Debug.Log("HpRegen == " + _player.Health.HpRegenerationValue);

        //_poisonBall.Buff.CastSpeed.IncreasePercentage(_increaseCastTime);
        //_spitPoison.Buff.CastSpeed.IncreasePercentage(_increaseCastTime);
        //_isCanCast = true;
    }

}
