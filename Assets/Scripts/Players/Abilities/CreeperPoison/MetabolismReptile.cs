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

    private float _originalHpRegen;
    private float _increaseHealthRegen = 2f;
    private float _increaseCastTime = 2f;
    private float _increaseCooldownTime = 2f;

    private bool _isCanCast = true;
    public bool Enabled;

    protected override bool IsCanCast => _isCanCast;

    protected override IEnumerator PrepareJob()
    {
        Debug.Log("MetabolismReptile / PrepareJob");
        _originalHpRegen = _player.Health.RegenerationValue;
        yield return null;
    }

    protected override IEnumerator CastJob()
    {
        Debug.Log("MetabolismReptile / CastJob");
        IncreaseValues();
        yield return null;
    }

    protected override void ClearData()
    {
        Debug.Log("MetabolismReptile / ClearData");
    }

    private void IncreaseValues()
    {
        Debug.Log("MetabolismReptile / IncreaseValues");

        float increasedHpRegen = _originalHpRegen * _increaseHealthRegen;
        _player.Health.RegenerationValue = increasedHpRegen;
        Debug.Log("HpRegen / IncreaseValues == " + _player.Health.RegenerationValue);

        Debug.Log($"MetabolismReptile / IncreaseValues / after newRemainingCooldownForSpitPoison = {_spitPoison.CooldownTime}");
        float newRemainingCooldownForSpitPoison = _spitPoison.CooldownTime / _increaseCooldownTime;
        _spitPoison.ReductionSetCooldown(newRemainingCooldownForSpitPoison);
        Debug.Log($"MetabolismReptile / IncreaseValues / before newRemainingCooldownForSpitPoison = {_spitPoison.CooldownTime}");
        //Сделать потом уменьшение кулдаунов зарядов для PoisonBall

        _poisonBall.Buff.CastSpeed.ReductionPercentage(_increaseCastTime);
        _spitPoison.Buff.CastSpeed.ReductionPercentage(_increaseCastTime);

        Invoke("ResetValues", _duration);
    }

    private void ResetValues()
    {
        Debug.Log("MetabolismReptile / IncreaseValues");
        _player.Health.RegenerationValue = _originalHpRegen;
        Debug.Log("HpRegen / ResetValues == " + _player.Health.RegenerationValue);

        _poisonBall.Buff.CastSpeed.IncreasePercentage(_increaseCastTime);
        _spitPoison.Buff.CastSpeed.IncreasePercentage(_increaseCastTime);
        _isCanCast = true;
    }

}
