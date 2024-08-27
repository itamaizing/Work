using DG.Tweening;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;

public class PoisonSlap : Skill
{
    #region Variables

    [SerializeField] private Character _player;

    [Header("Abilities")]
    [SerializeField] private PoisonBall _poisonBall;
    [SerializeField] private CreeperStrike _creeperStrike;
    [SerializeField] private LightningStrikes _lightningStrikes;
    [SerializeField] private LightweightSlap _lightweightSlap;

    private Character _currentTarget;

    private Vector3 _firstMousePosition = Vector3.positiveInfinity;
    private Vector3 _secondMousePosition;

    private float _creeperStrikeCastSpeedMultiplier = 0.5f; // Уменьшение скорости каста на 50%
    private float _lightningStrikesCastSpeedMultiplier = 0.0f;  // Уменьшение скорости каста на 100%
    private float _baseTimeCast = 1.6f;

    private float _baseDamage = 30f;
    private float _distancePush = 3.0f;
    private float _durationPush = 1.0f;

    private Coroutine _secondMouseClickCoroutine;
    private Coroutine _castSpeedFromCreeperStrikeCoroutine;
    private Coroutine _castSpeedFromLightningStrikesCoroutine;

    private bool _isPushTargetAllowed;
    private bool _secondClickDone;
    private bool _isIncreasedCastSpeedFromCreeperStrike = false;
    private bool _isIncreasedCastSpeedFromLightningStrike = false;

    public bool Enabled;
    protected override bool IsCanCast => true;

    #endregion

    #region PrepareAndStartJob

    protected override void ClearData()
    {
        _firstMousePosition = Vector3.positiveInfinity;
        _secondMousePosition = Vector3.zero;

        _secondClickDone = false;
        _isPushTargetAllowed = false;

        _currentTarget = null;
        _castDeley = 0;

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

    protected override IEnumerator PrepareJob()
    {
        while (_currentTarget == null)
        {
            if (Input.GetMouseButton(0))
            {
                _currentTarget = GetRaycastTarget();

                _firstMousePosition = GetMousePoint();
            }
            yield return null;
        }

        yield return _secondMouseClickCoroutine = StartCoroutine(SecondClick());

        if (_currentTarget != null)
        {
            if (_poisonBall.CurrentCharges != 0)
            {
                if ((_lightweightSlap.IsActive && _creeperStrike.IsTwoHit) || (_lightweightSlap.IsActive && _lightningStrikes.IsUsedLightningStrikes))
                {
                    yield break;
                }
                else
                {
                    _poisonBall.PayCostPoisonBall();
                }
            }
        }
    }

    protected override IEnumerator CastJob()
    {
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
            _castDeley = _baseTimeCast;
            yield return StartCastDeleyCoroutine();

            ChooseDirectionPush();

            DamageDeal();
        }
    }

    #endregion

    #region CalculationsDistances

    private void ChooseDirectionPush()
    {
        _isPushTargetAllowed = Vector2.Distance(_player.transform.position, _secondMousePosition) > Vector2.Distance(_player.transform.position, _currentTarget.transform.position);

    }

    #endregion

    #region Coroutines

    private IEnumerator SecondClick()
    {
        while (!_secondClickDone)
        {
            if (Input.GetMouseButtonDown(0))
            {
                _secondClickDone = true;
                _secondMousePosition = GetMousePoint();
            }
            yield return null;
        }
    }

    private IEnumerator CastSpeedFromCreeperStrike()
    {
        _creeperStrike.IsTwoHit = false;
        _isIncreasedCastSpeedFromCreeperStrike = true;

        float _timeCastFromCreeperStrike = _baseTimeCast * _creeperStrikeCastSpeedMultiplier;

        _castDeley = _timeCastFromCreeperStrike;
        yield return StartCastDeleyCoroutine();

        Debug.Log("CastTime int if == " + _castDeley);

        ChooseDirectionPush();

        DamageDeal();
    }

    private IEnumerator CastSpeedFromLightningStrikes()
    {
        _isIncreasedCastSpeedFromLightningStrike = true;

        float _timeCastFromLightningStrikes = _baseTimeCast * _lightningStrikesCastSpeedMultiplier;

        _castDeley = _timeCastFromLightningStrikes;
        yield return StartCastDeleyCoroutine();

        Debug.Log("CastTime int else if == " + _castDeley);

        ChooseDirectionPush();

        DamageDeal();
    }

    #endregion

    #region DamageDealAndPushTargetMethods

    private void DamageDeal()
    {
        if (_currentTarget != null) 
        {
            //_currentTarget.Health.CmdTryTakeDamage(Buff.Damage.GetBuffedValue(_baseDamage), DamageType.Physical, AttackRangeType.MeleeAttack);
            PushTarget(_currentTarget.gameObject, _distancePush, _durationPush, _isPushTargetAllowed);
        }
    }

    private void PushTarget(GameObject target, float distancePush, float durationPush, bool isCanPushTarget)
    {
        CmdPushEnemy(target, distancePush, durationPush, isCanPushTarget);
    }

    #endregion

    #region CommandMethods

    [Command]
    private void CmdPushEnemy(GameObject target, float distancePush, float durationPush, bool isCanPushTarget) 
    {
        Vector2 directionPush = (target.transform.position - transform.position);

        distancePush = ((distancePush * GlobalVariable.cellSize) * durationPush) / GlobalVariable.cellSize;
        if (isCanPushTarget)
        {
            target.GetComponent<Transform>().transform.DOMove((Vector2)target.transform.position + directionPush * distancePush, durationPush).SetEase(Ease.Linear);
        }
        else
        {
            target.GetComponent<Transform>().transform.DOMove((Vector2)target.transform.position - directionPush * distancePush, durationPush).SetEase(Ease.Linear);
        }
    }

    #endregion
}
