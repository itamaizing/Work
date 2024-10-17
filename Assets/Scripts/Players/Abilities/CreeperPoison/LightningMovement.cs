using DG.Tweening;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions.Must;

public class LightningMovement : Skill
{
    #region Variables

    [Header("Talents")]
    [SerializeField] private SuperFastScales _superFastScales;
    [SerializeField] private HeatedGlands _heatedGlands;
    private float _timeBuff = 4f;

    [Header("Abilities Player")]
    [SerializeField] private CreeperStrike _creeperStrike;
    [SerializeField] private LightningStrikes _lightningStrikes;
    [SerializeField] private PoisonSlap _poisonSlap;

    [Header("Ability properties")]
    [SerializeField] private AbilityLineRenderer _line;
    [SerializeField] private GameObject _midPointForLeapPrefab;
    [SerializeField] private Character _player;

    [SerializeField] private float _rangeLeap;
    [SerializeField] private float _durationLeap;
    [SerializeField] private float _radiusAttack;
    [SerializeField] private float _cooldownAttack;
    [SerializeField] private float _invtervalBetweenLeaps;
    public bool IsInMovement = false;

    private Character _target;
    private Dictionary<GameObject, float> _targetHitTimes = new Dictionary<GameObject, float>();
    private List<Skill> _skillList = new();

    private Vector3 _firstLeapPoint = Vector3.positiveInfinity;
    private Vector3 _secondLeapPoint;
    private Vector3 _firstLeapPointIfIsObstacle;
    private Vector3 _secondLeapPointIfIsObstacle;

    #region ForRenderingVectors

    [SerializeField] private Color _colorForEnd;
    [SerializeField] private Color _colorForStart;
    private BoxArea _lineStartImageForFirstLeap;
    private BoxArea _lineEndImageForFirstLeap;
    private BoxArea _lineStartImageForSecondLeap;
    private BoxArea _lineEndImageForSecondLeap;
    private GameObject _midPointForLeap;

    #endregion

    #region CoroutineVariables

    private Coroutine _firstPointForLeapCoroutine;
    private Coroutine _secondPointForLeapCoroutine;
    private Coroutine _midPointForLeapCoroutine;
    private Coroutine _renderLineForFirstLeapCoroutine;
    private Coroutine _renderLineForSecondLeapCoroutine;
    private Coroutine _isTargetBeforePlayerCoroutine;
    private Coroutine _isTargetBehindPlayerCoroutine;
    private Coroutine _isTargetOnEndPointCoroutine;
    private Coroutine _applyDamageCoroutine;

    #endregion

    private float _angle;
    private float _multiplierLeap;

    #region BoolVariables

    [SyncVar] private bool _isTargetBeforePlayer = false;
    [SyncVar] private bool _isTargetBehindPlayer = false;
    [SyncVar] private bool _isTargetOnEndPointSecondLeap = false;
    private bool _isFirstClickDone = false;
    private bool _isSecondClickDone = false;
    private bool _isTarget = false;
    private bool _isAbilityDone = false;
    private bool _heatedGlandsIsActive;
    #endregion

    public float RadiusAttacks => _radiusAttack;
    #endregion

    #region PrepareAndStartJob

    protected override bool IsCanCast => CheckCanCast();

    protected override void ClearData()
    {
        _player.Move.enabled = true;

        _isFirstClickDone = false;
        _isSecondClickDone = false;
        _isTarget = false;

        _firstLeapPoint = Vector3.positiveInfinity;
        _secondLeapPoint = Vector3.zero;
        _firstLeapPointIfIsObstacle = Vector3.zero;
        _secondLeapPointIfIsObstacle = Vector3.zero;

        StopRenderLine();

        if (_firstPointForLeapCoroutine != null)
        {
            StopCoroutine(_firstPointForLeapCoroutine);
            _firstPointForLeapCoroutine = null;
        }

        if (_secondPointForLeapCoroutine != null)
        {
            StopCoroutine(_secondPointForLeapCoroutine);
            _secondPointForLeapCoroutine = null;
        }

        if (_midPointForLeapCoroutine != null)
        {
            StopCoroutine(_midPointForLeapCoroutine);
            _midPointForLeapCoroutine = null;
        }

        if (_isAbilityDone)
        {
            float timer = (_durationLeap * _rangeLeap) * 1.1f;
            _isAbilityDone = false;
            Invoke("ResetBools", timer);
        }
    }

    private void ResetBools()
    {
        _targetHitTimes.Clear();
        _rangeLeap = 4.0f;

        _isTargetBeforePlayer = false;
        _isTargetBehindPlayer = false;
        _isTargetOnEndPointSecondLeap = false;

        IsInMovement = false;
        _poisonSlap.IsCanDamageDeal = false;
        _lightningStrikes.IsCanDamageDeal = false;

        _target = null; 

        if (_isTargetBeforePlayerCoroutine != null)
        {
            StopCoroutine(_isTargetBeforePlayerCoroutine);
            _isTargetBeforePlayerCoroutine = null;
        }

        if (_isTargetBehindPlayerCoroutine != null)
        {
            StopCoroutine(_isTargetBehindPlayerCoroutine);
            _isTargetBehindPlayerCoroutine = null;
        }

        if (_applyDamageCoroutine != null)
        {
            StopCoroutine(_applyDamageCoroutine);
            _applyDamageCoroutine = null;
        }

        if (_isTargetOnEndPointCoroutine != null)
        {
            StopCoroutine(_isTargetOnEndPointCoroutine);
            _isTargetOnEndPointCoroutine = null;
        }

        if (_superFastScales.Data.IsOpen)
        {
            _superFastScales.ResetResistance();
        }

    }

    protected override IEnumerator PrepareJob()
    {
        StopAutoDraw();
        _renderLineForFirstLeapCoroutine = StartCoroutine(RenderLineForFirstLeapJob(_castLength, _castWidth, _line, transform));

        if (_heatedGlands.Data.IsOpen)
        {
            _heatedGlandsIsActive = _heatedGlands.Data.IsOpen;
        }

        while (_target == null && float.IsPositiveInfinity(_firstLeapPoint.x))
        {
            if (GetMouseButton)
            {
                yield return _firstPointForLeapCoroutine = StartCoroutine(FirstVectorForLeap());
                IsEnemyBeforePlayer();

                if (_isFirstClickDone && _isTarget)
                {
                    yield return _midPointForLeapCoroutine = StartCoroutine(MidpointForRenderingSecondLeap(_firstLeapPoint));
                }
            }
            yield return null;
        }

    }

    protected override IEnumerator CastJob()
    {
        if (_isFirstClickDone && !_isTarget)
        {
            if (IsObstacle(transform.position, _firstLeapPoint))
            {
                _firstLeapPoint = _firstLeapPointIfIsObstacle;
            }
            SingleLeap(_firstLeapPoint);
        }
        else if (_isFirstClickDone && _isSecondClickDone && _isTarget)
        {
            if (IsObstacle(transform.position, _firstLeapPoint))
            {
                _firstLeapPoint = _firstLeapPointIfIsObstacle;
                SingleLeap(_firstLeapPoint);
            }
            else
            {
                if (IsObstacle(_firstLeapPoint, _secondLeapPoint))
                {
                    _secondLeapPoint = _secondLeapPointIfIsObstacle;
                }
                ExecuteLeaps(_firstLeapPoint, _secondLeapPoint);
            }
        }

        yield return null;
    }


    #endregion

    #region CheckMethods

    private Vector3 LimitFirstLeapToMaxDistance(Vector3 startPoint, Vector3 targetPoint, float maxDistance)
    {
        Vector3 direction = (targetPoint - startPoint).normalized;
        return startPoint + direction * maxDistance;
    }

    private Vector3 LimitSecondLeapToMaxDistance(Vector3 startPoint, Vector3 targetPoint, float maxDistance)
    {
        Debug.Log("LimitSecondLeap");
        Vector3 centerTarget;

        Vector3 direction = (targetPoint - startPoint).normalized;

        float dividerForMultiplier = 10f;
        float coefficientForMultiplier = 0.5f;

        if (_isTargetOnEndPointCoroutine == null)
        {
            _isTargetOnEndPointCoroutine = StartCoroutine(IsTargetOnEndPoint(targetPoint, _targetsLayers));
        }

        if (_target != null)
        {
            centerTarget = _target.GetComponent<Character>().Collider.bounds.center;
        }
        else
        {
            centerTarget = Vector3.zero;
        }

        bool isPointBehindCenterTarget = Vector2.Distance(startPoint, targetPoint) > Vector2.Distance(startPoint, centerTarget);

        if (_isTargetOnEndPointSecondLeap)
        {
            if (isPointBehindCenterTarget)
            {// Точка прыжка за серединой врага
                Debug.Log("if >");
                maxDistance += 1.5f;
            }
            else
            {// Точка прыжка перед серединой врага
                Debug.Log("else <");
                maxDistance += 2.8f;
            }
            _multiplierLeap = (maxDistance / dividerForMultiplier) + coefficientForMultiplier;
            Debug.Log("MultiplierLeap = " + _multiplierLeap);
            return startPoint + direction * maxDistance;
        }
        else
        {
            _multiplierLeap = (maxDistance / dividerForMultiplier) + coefficientForMultiplier;
            return startPoint + direction * maxDistance;
        }
    }

    private bool CheckCanCast()
    {
        if (_target == null)
            return Vector3.Distance(_firstLeapPoint, transform.position) <= Radius;

        return Vector3.Distance(_firstLeapPoint, transform.position) <= Radius ||
               Vector3.Distance(_target.transform.position, transform.position) <= Radius;
    }

    private bool IsObstacle(Vector3 startPos, Vector3 endPos)
    {
        RaycastHit2D hitObstacle = Physics2D.Linecast(startPos, endPos, _obstacle);
        if (hitObstacle.collider != null)
        {
            _firstLeapPointIfIsObstacle = hitObstacle.point;
            _secondLeapPointIfIsObstacle = hitObstacle.point;
        }
        return hitObstacle.collider != null;
    }

    private void IsEnemyBeforePlayer()
    {
        float castLengthMultiplier = 4f;
        float castWidthMultiplier = 1.55f;

        Vector2 sizeBox = new Vector2(_castLength * castLengthMultiplier, _castWidth * castWidthMultiplier);

        Collider2D hit = Physics2D.OverlapBox(transform.position, sizeBox, _angle, _targetsLayers);
        if (hit != null)
        {
            _isTarget = true;
        }
        else
        {
            _isTarget = false;
        }
    }

    private IEnumerator IsTargetOnEndPoint(Vector2 secondLeap, LayerMask targetLayer)
    {
        while (true)
        {
            Debug.Log("IsTargetOnEndPoint");
            Collider2D hit = Physics2D.OverlapCircle(secondLeap, 1.35f, targetLayer);
            if (hit != null)
            {
                _target = hit.gameObject.GetComponent<Character>();

                _isTargetOnEndPointSecondLeap = true;
            }
            yield return null;
        }
    }

    private IEnumerator IsTargetBeforePlayerJob(float rangeLeap, LayerMask targetLayer)
    {
        Vector2 sizeBox = new Vector2(_castLength, _castWidth);
        while (true)
        {
            Collider2D hit = Physics2D.OverlapBox(transform.position, sizeBox, _angle - 90f, targetLayer);
            if (hit != null)
            {
                _isTargetBeforePlayer = true;
            }

            yield return null;
        }
    }

    private IEnumerator IsTargetBehindPlayerJob(float rangeLeap, LayerMask targetLayer)
    {
        Vector2 sizeBox = new Vector2(_castLength * 1.2f, _castWidth * 1.2f);
        while (true)
        {
            Collider2D hit = Physics2D.OverlapBox(transform.position, sizeBox, _angle + 90f, targetLayer);
            if (hit != null)
            {
                _isTargetBehindPlayer = true;
            }

            yield return null;
        }
    }

    #endregion

    #region RenderingLine

    private void RotateAtMouse(Transform transform)
    {
        Vector3 dir = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
        _angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, _angle - 90f);
    }

    private IEnumerator RenderLineForFirstLeapJob(float length, float width, AbilityLineRenderer line, Transform lineTransform)
    {
        Transform transformLine = lineTransform;

        _lineStartImageForFirstLeap = Instantiate(line.Start, transformLine);
        _lineEndImageForFirstLeap = Instantiate(line.End, transformLine);

        _lineStartImageForFirstLeap.SetColor(_colorForStart);
        _lineEndImageForFirstLeap.SetColor(_colorForEnd);

        while (!_isFirstClickDone)
        {
            RotateAtMouse(_lineStartImageForFirstLeap.transform);
            RotateAtMouse(_lineEndImageForFirstLeap.transform);

            Damage damage = new Damage
            {
                Value = 0f,
                Type = DamageType.Physical,
                Range = AttackRangeType.MeleeAttack,
            };

            _lineStartImageForFirstLeap.SetSize(width, length, damage);
            _lineEndImageForFirstLeap.SetSize(width, length, damage);

            yield return null;
        }
    }

    private IEnumerator RenderLineForSecondLeapJob(float length, float width, AbilityLineRenderer line, Transform lineTransform)
    {
        Transform transformLine = lineTransform;

        _lineStartImageForSecondLeap = Instantiate(line.Start, transformLine);
        _lineEndImageForSecondLeap = Instantiate(line.End, transformLine);

        _lineStartImageForSecondLeap.SetColor(_colorForStart);
        _lineEndImageForSecondLeap.SetColor(_colorForEnd);

        while (!_isSecondClickDone)
        {
            RotateAtMouse(_lineStartImageForSecondLeap.transform);
            RotateAtMouse(_lineEndImageForSecondLeap.transform);

            Damage damage = new Damage
            {
                Value = 0f,
                Type = DamageType.Physical,
                Range = AttackRangeType.MeleeAttack,
            };

            _lineStartImageForSecondLeap.SetSize(width, length, damage);
            _lineEndImageForSecondLeap.SetSize(width, length, damage);

            yield return null;
        }
    }

    private void StopRenderLine()
    {
        if (_renderLineForFirstLeapCoroutine != null)
        {
            StopCoroutine(_renderLineForFirstLeapCoroutine);
            _renderLineForFirstLeapCoroutine = null;
        }

        if (_renderLineForSecondLeapCoroutine != null)
        {
            StopCoroutine(_renderLineForSecondLeapCoroutine);
            _renderLineForSecondLeapCoroutine = null;
        }

        if (_lineStartImageForFirstLeap != null)
            Destroy(_lineStartImageForFirstLeap.gameObject);

        if (_lineEndImageForFirstLeap != null)
            Destroy(_lineEndImageForFirstLeap.gameObject);

        if (_lineStartImageForSecondLeap != null)
            Destroy(_lineStartImageForSecondLeap.gameObject);

        if (_lineEndImageForSecondLeap != null)
            Destroy(_lineEndImageForSecondLeap.gameObject);

    }

    #endregion

    #region Coroutines

    private IEnumerator FirstVectorForLeap()
    {
        while (!_isFirstClickDone)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 rawFirstLeapPoint = GetMousePoint();

                _isFirstClickDone = true;
                _firstLeapPoint = LimitFirstLeapToMaxDistance(transform.position, rawFirstLeapPoint, _rangeLeap);
            }
            yield return null;
        }
    }

    private IEnumerator SecondVectorForLeap()
    {
        while (!_isSecondClickDone)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 rawSecondLeapPoint = GetMousePoint();

                _isSecondClickDone = true;
                _target = GetRaycastTarget();
                _secondLeapPoint = LimitSecondLeapToMaxDistance(_firstLeapPoint, rawSecondLeapPoint, _rangeLeap);
            }
            yield return null;
        }
    }

    private IEnumerator MidpointForRenderingSecondLeap(Vector2 firstLeapPoint)
    {
        Vector3 originalPoint = firstLeapPoint;

        _midPointForLeap = Instantiate(_midPointForLeapPrefab, originalPoint, Quaternion.identity);

        _renderLineForSecondLeapCoroutine = StartCoroutine(RenderLineForSecondLeapJob(_castLength, _castWidth, _line, _midPointForLeap.transform));

        yield return _secondPointForLeapCoroutine = StartCoroutine(SecondVectorForLeap());

        Destroy(_midPointForLeap.gameObject);

        StopRenderLine();
    }

    private IEnumerator ApplyDamageJob(LayerMask enemiesLayer, float radius, float duration)
    {
        while (true)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, enemiesLayer);

            foreach (Collider2D item in hits)
            {
                if (item != null)
                {
                    GameObject target = item.gameObject;
                    var targetCharacter = item.gameObject.GetComponent<Character>();

                    if (_targetHitTimes.ContainsKey(target))
                    {
                        if (Time.time - _targetHitTimes[target] < _cooldownAttack)
                        {
                            continue;
                        }
                    }

                    bool isAtFirstLeapEnd = Vector2.Distance(target.transform.position, _firstLeapPoint) < 0.1f;
                    bool isAtSecondLeapStart = Vector2.Distance(target.transform.position, _secondLeapPoint) < 0.1f;

                    if (!isAtFirstLeapEnd && !isAtSecondLeapStart)
                    {
                        if (_lightningStrikes.IsCanDamageDeal)
                        {
                            _lightningStrikes.UseLightningStrikesOfLightningMovement(targetCharacter, duration);
                        }
                        else if (_poisonSlap.IsCanDamageDeal)
                        {
                            _poisonSlap.DamageDealOfLightningMovement(targetCharacter, duration);
                        }
                        else
                        {
                            _creeperStrike.DealingDamageFromHits(targetCharacter);
                        }

                        _targetHitTimes[target] = Time.time;

                    }
                }
            }
            yield return null;
        }
    }

    #endregion

    #region Leaps
    private void SingleLeap(Vector2 firstLeapPoint)
    {
        _isAbilityDone = true;
        IsInMovement = true;

        _player.Move.enabled = false;

        if (_isTargetBeforePlayerCoroutine == null)
        {
            _isTargetBeforePlayerCoroutine = StartCoroutine(IsTargetBeforePlayerJob(_rangeLeap, _targetsLayers));
        }

        if (_superFastScales.Data.IsOpen)
        {
            _superFastScales.IncreasingResistance();
        }

        _player.CharacterState.CmdAddState(States.Immateriality, (_durationLeap * _rangeLeap * _multiplierLeap), 0, _player.gameObject, Name);

        CmdSingleLeap(firstLeapPoint, _durationLeap, _rangeLeap, _multiplierLeap, _timeBuff, _heatedGlandsIsActive);
    }

    private void ExecuteLeaps(Vector2 firstLeapPoint, Vector2 secondLeapPoint)
    {
        _isAbilityDone = true;
        IsInMovement = true;
        _player.Move.enabled = false;

        if (_isTargetBeforePlayerCoroutine == null)
        {
            _isTargetBeforePlayerCoroutine = StartCoroutine(IsTargetBeforePlayerJob(_rangeLeap, _targetsLayers));
        }
        if (_isTargetBehindPlayerCoroutine == null)
        {
            _isTargetBehindPlayerCoroutine = StartCoroutine(IsTargetBehindPlayerJob(_rangeLeap, _targetsLayers));
        }

        if (_superFastScales.Data.IsOpen)
        {
            _superFastScales.IncreasingResistance();
        }

        _applyDamageCoroutine = StartCoroutine(ApplyDamageJob(_targetsLayers, _radiusAttack, _durationLeap * _rangeLeap));

        _player.CharacterState.CmdAddState(States.Immateriality, (_durationLeap * _rangeLeap * _multiplierLeap), 0, _player.gameObject, Name);

        CmdExecuteTwoLeaps(firstLeapPoint, secondLeapPoint, 
            _durationLeap, _rangeLeap, _multiplierLeap, _timeBuff, _invtervalBetweenLeaps,
            _heatedGlandsIsActive, _targetsLayers);
    }

    #endregion

    #region Command

    [Command]
    private void CmdSingleLeap(Vector2 firstLeapPoint, 
        float durationLeap, float rangeLeap, float multiplierLeap, float timeBuff, 
        bool heatedGlandsIsAcitve)
    {
        _player.Move.enabled = false;

        MoveComponent playerTransform = _player.GetComponent<MoveComponent>();

        playerTransform.TargetRpcDoMove(firstLeapPoint, (durationLeap * rangeLeap / GlobalVariable.cellSize));
    }

    [Command]
    private void CmdExecuteTwoLeaps(Vector2 firstLeapPoint, Vector2 secondLeapPoint,
        float durationLeap, float rangeLeap, float multiplierLeap, float timeBuff, float interval,
        bool heatedGlandsIsActive,
        LayerMask enemyLayer)
    {
        _player.Move.enabled = false;

        TargetRpcExecuteLeaps(_player.gameObject, firstLeapPoint, secondLeapPoint, 
            durationLeap, rangeLeap, multiplierLeap, timeBuff, interval,
            heatedGlandsIsActive, enemyLayer);
    }

    #endregion

    [TargetRpc]
    private void TargetRpcExecuteLeaps(GameObject player, Vector2 firstLeapPoint, Vector2 secondLeapPoint,
        float durationLeap, float rangeLeap, float multiplierLeap, float timeBuff, float interval,
        bool heatedGlandsIsActive,
        LayerMask enemyLayer)
    {
        Character playerRigidbody = player.GetComponent<Character>(); 

        Sequence leapSequence = DOTween.Sequence();

        leapSequence.AppendCallback(() =>
        {
            playerRigidbody.Rb.DOMove(firstLeapPoint, (durationLeap * rangeLeap / GlobalVariable.cellSize));
        });

        leapSequence.AppendInterval(interval);

        leapSequence.AppendCallback(() =>
        {
            if (_isTargetOnEndPointSecondLeap)
            {
                playerRigidbody.Rb.DOMove(secondLeapPoint, (durationLeap * rangeLeap / GlobalVariable.cellSize));

                if (heatedGlandsIsActive)
                {
                    _player.CharacterState.AddState(States.HeatedGlands, 4, 0, _player.gameObject, null);
                }
            }
            else
            {
                playerRigidbody.Rb.DOMove(secondLeapPoint, (durationLeap * rangeLeap / GlobalVariable.cellSize));
                if (heatedGlandsIsActive)
                {
                    _player.CharacterState.AddState(States.HeatedGlands, 4, 0, _player.gameObject, null);
                }
            }
        });
    }
}
