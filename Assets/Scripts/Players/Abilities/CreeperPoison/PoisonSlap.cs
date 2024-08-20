using DG.Tweening;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;

public class PoisonSlap : Skill
{
    [SerializeField] private Character _dad;

    [Header("Abilities")]
    [SerializeField] private PoisonBall _poisonBall;
    [SerializeField] private CreeperStrike _creeperStrike;
    [SerializeField] private LightningStrikes _lightningStrikes;

    [SerializeField] private LightweightSlap _lightweightSlap;

    private Character _currentTarget;

    private float _increasingExecutionSpeedFromCreeperStrike = 0.5f; // Уменьшение скорости каста на 50%
    private float _increasingExecutionSpeedFromLightningStrikes = 0.0f;  // Уменьшение скорости каста на 100%
    private float _baseTimeCast = 1.6f;

    private float _baseDamage = 30f;
    private float _distancePush = 3.0f;
    private float _durationPushInSeconds = 1.0f;

    private Coroutine _castSpeedFromCreeperStrikeCoroutine;
    private Coroutine _castSpeedFromLightningStrikesCoroutine;

    private bool _isIncreasedCastSpeedFromCreeperStrike = false;
    private bool _isIncreasedCastSpeedFromLightningStrike = false;

    public bool Enabled;

    protected override bool IsCanCast => CheckDistance();

    protected override IEnumerator PrepareJob()
    {
        while (_currentTarget == null)
        {
            if (Input.GetMouseButton(0))
            {
                _currentTarget = GetRaycastTarget();
            }
            yield return null;
        }
    }

    protected override IEnumerator CastJob()
    {
        if (_currentTarget != null)
        {
            TryPayCost();

            if (_poisonBall.CurrentCharges != 0)
            {
                if ((_lightweightSlap.IsActive && _creeperStrike.IsTwoHit) || (_lightweightSlap.IsActive && _lightningStrikes.IsUsedLightningStrikes))
                {
                    Debug.Log("IsActive true and Two hit");
                    yield return null;
                }
                else
                {
                    Debug.Log("charge --");
                    _poisonBall.PayCostPoisonBall();
                }
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
                yield return StartCastDelayCoroutine();
                DamageDeal();
            }
        }
    }

    protected override void ClearData()
    {
        _currentTarget = null;
        _castDelay = 0;

        _isIncreasedCastSpeedFromCreeperStrike = false;
        _isIncreasedCastSpeedFromLightningStrike = false;

        if (_castSpeedFromCreeperStrikeCoroutine != null)
        {
            StopCoroutine(CastSpeedFromCreeperStrike());
            _castSpeedFromCreeperStrikeCoroutine = null;
        }
        if (_castSpeedFromLightningStrikesCoroutine != null)
        {
            StopCoroutine(CastSpeedFromLightningStrikes());
            _castSpeedFromLightningStrikesCoroutine = null;
        }
    }

    private bool CheckDistance()
    {
        if (_currentTarget == null)
        {
            return Vector3.Distance(_currentTarget.transform.position, transform.position) <= Radius;
        }
        return Vector3.Distance(_currentTarget.transform.position, transform.position) <= Radius;
    }

    private IEnumerator CastSpeedFromCreeperStrike()
    {
        _creeperStrike.IsTwoHit = false;
        _isIncreasedCastSpeedFromCreeperStrike = true;

        float _timeCastFromCreeperStrike = _baseTimeCast * _increasingExecutionSpeedFromCreeperStrike;

        _castDelay = _timeCastFromCreeperStrike;
        yield return StartCastDelayCoroutine();
        Debug.Log("CastTime int if == " + _castDelay);

        DamageDeal();
    }

    private IEnumerator CastSpeedFromLightningStrikes()
    {
        _isIncreasedCastSpeedFromLightningStrike = true;

        float _timeCastFromLightningStrikes = _baseTimeCast * _increasingExecutionSpeedFromLightningStrikes;

        _castDelay = _timeCastFromLightningStrikes;
        yield return StartCastDelayCoroutine();
        Debug.Log("CastTime int else if == " + _castDelay);

        DamageDeal();
    }

    private void DamageDeal()
    {
        if (_currentTarget != null) 
        {
            _currentTarget.Health.CmdTryTakeDamage(Buff.Damage.GetBuffedValue(_baseDamage), DamageType.Physical, AttackRangeType.MeleeAttack);
            PushEnemy(_currentTarget.gameObject, _distancePush, _durationPushInSeconds);
        }
        ClearData();
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
