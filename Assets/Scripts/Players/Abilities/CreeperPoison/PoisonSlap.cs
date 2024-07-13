using DG.Tweening;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoisonSlap : TargetOrAreaAbility
{
    [SerializeField] private Character _dad;
    [SerializeField] private PoisonBall _poisonBall;
    [SerializeField] private CreeperStrike _creeperStrike;
    [SerializeField] private LightningStrikes _lightningStrikes;

    private float _increasingExecutionSpeedFromCreeperStrike = 0.5f; // Уменьшение скорости каста на 50%
    private float _increasingExecutionSpeedFromLightningStrikes = 0.0f;  // Уменьшение скорости каста на 100%
    private float _baseTimeCast = 1.6f;

    private float _baseDamage = 30f;
    private float _distancePush = 3.0f;
    private float _durationPushInSeconds = 1.0f;

    private Coroutine _useCoroutine;
    private Coroutine _castSpeedFromCreeperStrikeCoroutine;
    private Coroutine _castSpeedFromLightningStrikesCoroutine;


    private GameObject _currentTarget;

    private bool _isIncreasedCastSpeedFromCreeperStrike = false;
    private bool _isIncreasedCastSpeedFromLightningStrike = false;

    protected override void CastAction()
    {
        _useCoroutine = StartCoroutine(UseAbilityCoroutine());
    }

    protected override void Cancel()
    {
        _castDelay = 0;

        _isIncreasedCastSpeedFromCreeperStrike = false;
        _isIncreasedCastSpeedFromLightningStrike = false;

        if (_useCoroutine != null)
            StopCoroutine(UseAbilityCoroutine());

        if (_castSpeedFromCreeperStrikeCoroutine != null)
            StopCoroutine(CastSpeedFromCreeperStrike());

        if (_castSpeedFromLightningStrikesCoroutine != null)
            StopCoroutine(CastSpeedFromLightningStrikes());
        //if (_damageDealCoroutine != null)
        //    StopCoroutine(DamageDeal());
    }

    private IEnumerator UseAbilityCoroutine()
    {
        PayCost();
        if (_poisonBall.CurrentCharges != 0)
        {
            _poisonBall.CurrentCharges--;
            yield return null;
        }

        if (_creeperStrike.IsTwoHit && !_isIncreasedCastSpeedFromLightningStrike)
        {
            _castSpeedFromCreeperStrikeCoroutine = StartCoroutine(CastSpeedFromCreeperStrike());
        }
        else if (_lightningStrikes.IsUsedLightningStrikes && !_isIncreasedCastSpeedFromCreeperStrike)
        {
            _castSpeedFromLightningStrikesCoroutine = StartCoroutine(CastSpeedFromLightningStrikes());
        }
        else
        {
            _castDelay = _baseTimeCast;
            yield return GetCastDeleyCoroutine();
            DamageDeal();
        }
    }


    private IEnumerator CastSpeedFromCreeperStrike()
    {
        _creeperStrike.IsTwoHit = false;
        _isIncreasedCastSpeedFromCreeperStrike = true;

        float _timeCastFromCreeperStrike = _baseTimeCast * _increasingExecutionSpeedFromCreeperStrike;

        _castDelay = _timeCastFromCreeperStrike;
        yield return GetCastDeleyCoroutine();
        Debug.Log("CastTime int if == " + _castDelay);

        DamageDeal();
    }

    private IEnumerator CastSpeedFromLightningStrikes()
    {
        _isIncreasedCastSpeedFromLightningStrike = true;

        float _timeCastFromLightningStrikes = _baseTimeCast * _increasingExecutionSpeedFromLightningStrikes;

        _castDelay = _timeCastFromLightningStrikes;
        yield return GetCastDeleyCoroutine();
        Debug.Log("CastTime int else if == " + _castDelay);

        DamageDeal();
    }

    private void DamageDeal()
    {
        _currentTarget = Target.gameObject;

        if (_currentTarget != null) 
        {
            CmdApplyDamage(_currentTarget.gameObject, _baseDamage, DamageType.Physical, AttackRangeType.MeleeAttack);
            PushEnemy(_currentTarget, _distancePush, _durationPushInSeconds);
        }

        Cancel();
    }

    private void PushEnemy(GameObject target, float distancePush, float durationPush)
    {
        CmdPushEnemy(target, distancePush, durationPush);
    }

    [Command]
    private void CmdPushEnemy(GameObject target, float distancePush, float durationPush) 
    {
        Vector2 directionPush = (target.transform.position - transform.position);

        distancePush = ((distancePush * GlobalVariable.cellSize) * durationPush) / GlobalVariable.cellSize;
        target.GetComponent<Transform>().transform.DOMove((Vector2)target.transform.position + directionPush * distancePush, durationPush).SetEase(Ease.Linear);
    }
}
