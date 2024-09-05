using DG.Tweening;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;

public class PoisonSlap : Skill
{
    #region Variables

    public bool Enabled;

    [SerializeField] private Character _player;

    [Header("Abilities")]
    [SerializeField] private PoisonBall _poisonBall;
    [SerializeField] private CreeperStrike _creeperStrike;
    [SerializeField] private LightningStrikes _lightningStrikes;

    [Header("Talents")]
    [SerializeField] private LightweightSlap _lightweightSlap;

    #region DisplayArrow

    [SerializeField] private GameObject _arrowPrefab;

    private GameObject[] _arrowRenderers = new GameObject[2]; 
    private bool _colorLockedAfterSecondClick = false;
    private bool _colorLockedAfterThirdClick = false;

    #endregion

    private Character _currentTarget;

    private Vector3 _firstMousePosition = Vector3.positiveInfinity;
    private Vector3 _secondMousePosition;

    private float _creeperStrikeCastSpeedMultiplier = 0.5f; // ���������� �������� ����� �� 50%
    private float _lightningStrikesCastSpeedMultiplier = 0.0f;  // ���������� �������� ����� �� 100%
    private float _baseTimeCast = 1.6f;

    private float _baseDamage = 30f;
    private float _distancePush = 3.0f;
    private float _durationPush = 1.0f;

    private Coroutine _secondMouseClickCoroutine;
    private Coroutine _castSpeedFromCreeperStrikeCoroutine;
    private Coroutine _castSpeedFromLightningStrikesCoroutine;

    private bool _isPushTargetAllowed;
    private bool _firstClickDone = false;
    private bool _secondClickDone;
    private bool _isIncreasedCastSpeedFromCreeperStrike = false;
    private bool _isIncreasedCastSpeedFromLightningStrike = false;

    protected override bool IsCanCast => true;

    #endregion

    private void Update()
    {
        UpdateMouseDetection();
    }

    #region PrepareAndStartJob

    protected override void ClearData()
    {
        ClearArrows();

        _firstMousePosition = Vector3.positiveInfinity;
        _secondMousePosition = Vector3.zero;

        _firstClickDone = false;
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
                if (_currentTarget != null)
                {
                    CreateArrowsParallelToPlayer();
                    StopAutoDraw();
                }
                _firstClickDone = true;
            }
            yield return null;
        }

        yield return _secondMouseClickCoroutine = StartCoroutine(SecondClick());
    }

    protected override IEnumerator CastJob()
    {
       /* if (_currentTarget != null)
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
*/
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

    #region ArrowManagement

    private void CreateArrowsParallelToPlayer()
    {
        if (_currentTarget == null || _arrowPrefab == null)
        {
            Debug.LogError("Arrow Prefab is not assigned or Target is null");
            return;
        }

        Vector3 targetPosition = _currentTarget.transform.position;
        Vector3 playerPosition = _player.transform.position;

        Vector3 directionToTarget = (targetPosition - playerPosition).normalized;

        Vector3 perpendicularDirection = Vector3.Cross(directionToTarget, Vector3.forward).normalized;

        _arrowRenderers[0] = Instantiate(_arrowPrefab, targetPosition + directionToTarget, Quaternion.identity);
        _arrowRenderers[1] = Instantiate(_arrowPrefab, targetPosition - directionToTarget, Quaternion.identity);

        SetArrowDirections(perpendicularDirection);
        SetArrowColors(Color.red);
    }

    private void SetArrowDirections(Vector3 perpendicularDirection)
    {
        for (int i = 0; i < _arrowRenderers.Length; i++)
        {
            var arrow = _arrowRenderers[i];
            if (arrow != null)
            {
                var drawArrow = arrow.GetComponent<DrawArrow>();
                if (drawArrow != null)
                {
                    Vector3 startPoint = arrow.transform.position - perpendicularDirection * 0.5f;
                    Vector3 endPoint = arrow.transform.position + perpendicularDirection * 0.5f;

                    if (i % 2 == 0)
                    {
                        drawArrow.DrawCurvedArrow(startPoint, endPoint, true);
                    }
                    else
                    {
                        drawArrow.DrawCurvedArrow(startPoint, endPoint, false);
                    }
                }
            }
        }
    }

    private void SetArrowColors(Color color)
    {
        foreach (var arrow in _arrowRenderers)
        {
            if (arrow != null)
            {
                var lineRenderer = arrow.GetComponent<LineRenderer>();
                if (lineRenderer != null)
                {
                    lineRenderer.startColor = color;
                    lineRenderer.endColor = color;
                }
            }
        }
    }

    private void SetArrowColor(int arrowIndex, Color color)
    {
        if (_arrowRenderers[arrowIndex] != null)
        {
            var lineRenderer = _arrowRenderers[arrowIndex].GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                lineRenderer.startColor = color;
                lineRenderer.endColor = color;
            }
        }
    }

    private void DarkenArrowColor(int arrowIndex, float alpha)
    {
        if (_arrowRenderers[arrowIndex] != null)
        {
            var lineRenderer = _arrowRenderers[arrowIndex].GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                Color startColor = lineRenderer.startColor;
                Color endColor = lineRenderer.endColor;

                startColor.a = alpha;
                endColor.a = alpha;

                lineRenderer.startColor = startColor;
                lineRenderer.endColor = endColor;
            }
        }
    }

    private void ClearArrows()
    {
        foreach (var arrow in _arrowRenderers)
        {
            if (arrow != null)
            {
                Destroy(arrow);
            }
        }

        Debug.Log("Arrows cleared.");
    }

    #endregion

    #region Update Method for Mouse Movement Detection

    private void UpdateMouseDetection()
    {
        if (_firstClickDone && !_secondClickDone)
        {
            Vector3 currentMousePosition = GetMousePoint();
            if (currentMousePosition.x < _firstMousePosition.x)
            {
                SetArrowColor(0, Color.green);
                SetArrowColor(1, Color.red);
            }
            else
            {
                SetArrowColor(0, Color.red);
                SetArrowColor(1, Color.green);
            }
        }
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
                if (_currentTarget != null)
                {
                    if (_secondMousePosition.x < _firstMousePosition.x)
                    {
                        DarkenArrowColor(0, 0.8f);
                        DarkenArrowColor(1, 0f);
                    }
                    else
                    {
                        DarkenArrowColor(0, 0f);
                        DarkenArrowColor(1, 0.8f);
                    }
                }
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
