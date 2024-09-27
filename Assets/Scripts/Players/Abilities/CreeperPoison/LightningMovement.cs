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
    [SerializeField] private AcceleratedSlap _acceleratedSlap;
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

    public bool IsInMovement = false;

    private GameObject _target;
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

        if (_superFastScales.IsActive)
        {
            _superFastScales.ResetResistance();
        }

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


    protected override IEnumerator PrepareJob()
    {
        StopAutoDraw();
        _renderLineForFirstLeapCoroutine = StartCoroutine(RenderLineForFirstLeapJob(_castLength, _castWidth, _line, transform));

        if (_heatedGlands.IsActive)
        {
            _heatedGlandsIsActive = _heatedGlands.IsActive;
        }

        while (_target == null && float.IsPositiveInfinity(_firstLeapPoint.x))
        {
            if (Input.GetMouseButtonDown(0))
            {
                yield return _firstPointForLeapCoroutine = StartCoroutine(FirstVectorForLeap());
                IsEnemyBeforePlayer();

                if (_isFirstClickDone && _isTarget)
                {
                    Debug.Log($"LightningMovement / PrepareJob / while / if (_isTarget ( == {_isTarget}))");
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
            SingleLeap(_firstLeapPoint, _durationLeap, _rangeLeap);
        }
        else if (_isFirstClickDone && _isSecondClickDone && _isTarget)
        {
            if (IsObstacle(transform.position, _firstLeapPoint))
            {
                _firstLeapPoint = _firstLeapPointIfIsObstacle;
                SingleLeap(_firstLeapPoint, _durationLeap, _rangeLeap);
            }
            else
            {
                if (IsObstacle(_firstLeapPoint, _secondLeapPoint))
                {
                    _secondLeapPoint = _secondLeapPointIfIsObstacle;
                }
                ExecuteLeaps(_firstLeapPoint, _secondLeapPoint, _durationLeap, _rangeLeap);
            }
        }

        yield return null;
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

        if (_superFastScales.IsActive)
        {
            _superFastScales.ResetResistance();
        }

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
        Vector3 centerTarget;
        Debug.Log("LightningMove / LimitSecondLeap");

        if (_isTargetOnEndPointCoroutine == null)
        {
            _isTargetOnEndPointCoroutine = StartCoroutine(IsTargetOnEndPoint(targetPoint, _targetsLayers));
        }

        Vector3 direction = (targetPoint - startPoint).normalized;
        if (_target != null)
        {
            centerTarget = _target.GetComponent<Character>().Collider.bounds.center;
        }
        else
        {
            centerTarget = Vector3.zero;
        }

        bool isPointBehindCenterTarget = Vector2.Distance(startPoint, targetPoint) > Vector2.Distance(startPoint, centerTarget);

        Debug.Log("isPointBehindCenterTarget = " + isPointBehindCenterTarget);
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
                maxDistance += 3f;
            }
            return startPoint + direction * maxDistance;
        }
        else
        {
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
            Debug.Log("LightningMovement / IsEnemyBeforePlayer / if hit == " + hit.gameObject.name);
            _isTarget = true;
        }
        else
        {
            Debug.Log("LightningMovement / IsEnemyBeforePlayer / else");
            _isTarget = false;
        }
    }

    private IEnumerator IsTargetOnEndPoint(Vector2 secondLeap, LayerMask targetLayer)
    {
        while (true)
        {
            Collider2D hit = Physics2D.OverlapCircle(secondLeap, 1.35f, targetLayer);
            if (hit != null)
            {
                _target = hit.gameObject;
                _isTargetOnEndPointSecondLeap = true;
                Debug.Log("IsTargetOnEndPoint = " + _isTargetOnEndPointSecondLeap);
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
                Debug.Log("LightningMove / IsTargetBeforePlayer / target = " + _target);
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
        Debug.Log("RenderLine length = " + length + "; width = " + width);
        Transform transformLine = lineTransform;

        _lineStartImageForFirstLeap = Instantiate(line.Start, transformLine);
        _lineEndImageForFirstLeap = Instantiate(line.End, transformLine);

        _lineStartImageForFirstLeap.SetColor(_colorForStart);
        _lineEndImageForFirstLeap.SetColor(_colorForEnd);

        while (!_isFirstClickDone)
        {
            RotateAtMouse(_lineStartImageForFirstLeap.transform);
            RotateAtMouse(_lineEndImageForFirstLeap.transform);

            _lineStartImageForFirstLeap.SetSize(width, length);
            _lineEndImageForFirstLeap.SetSize(width, length);

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

            _lineStartImageForSecondLeap.SetSize(width, length);
            _lineEndImageForSecondLeap.SetSize(width, length);

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
                Debug.Log("FirstLeapPoint = " + _firstLeapPoint);
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

    private IEnumerator ApplyDamageJob(LayerMask enemiesLayer, float radius)
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
                            _lightningStrikes.UseLightningStrikes(targetCharacter);
                        }
                        else if (_poisonSlap.IsCanDamageDeal)
                        {
                            Debug.Log("LightningMovement / ApplyDamage / else if poisonSlap");
                            _poisonSlap.DamageDeal(targetCharacter);
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
    private void SingleLeap(Vector2 firstLeapPoint, float durationLeap, float rangeLeap)
    {
        _isAbilityDone = true;
        IsInMovement = true;

        _player.Move.enabled = false;

        if (_isTargetBeforePlayerCoroutine == null)
        {
            _isTargetBeforePlayerCoroutine = StartCoroutine(IsTargetBeforePlayerJob(rangeLeap, _targetsLayers));
        }

        if (_superFastScales.IsActive)
        {
            _superFastScales.IncreasingResistance();
        }

        CmdSingleLeap(firstLeapPoint, durationLeap, rangeLeap, _timeBuff, _heatedGlandsIsActive);
    }

    private void ExecuteLeaps(Vector2 firstLeapPoint, Vector2 secondLeapPoint, float durationLeap, float rangeLeap)
    {
        _isAbilityDone = true;
        IsInMovement = true;
        _player.Move.enabled = false;

        if (_isTargetBeforePlayerCoroutine == null)
        {
            _isTargetBeforePlayerCoroutine = StartCoroutine(IsTargetBeforePlayerJob(rangeLeap, _targetsLayers));
        }
        if (_isTargetBehindPlayerCoroutine == null)
        {
            _isTargetBehindPlayerCoroutine = StartCoroutine(IsTargetBehindPlayerJob(rangeLeap, _targetsLayers));
        }

        if (_superFastScales.IsActive)
        {
            _superFastScales.IncreasingResistance();
        }

        _applyDamageCoroutine = StartCoroutine(ApplyDamageJob(_targetsLayers, _radiusAttack));

        CmdExecuteTwoLeaps(firstLeapPoint, secondLeapPoint, durationLeap, rangeLeap, _timeBuff, _heatedGlandsIsActive, _targetsLayers);
    }

    #endregion

    #region Command

    [Command]
    private void CmdSingleLeap(Vector2 firstLeapPoint, float durationLeap, float rangeLeap, float timeBuff, bool heatedGlandsIsAcitve)
    {
        MoveComponent playerTransform = _player.GetComponent<MoveComponent>();
        _player.CharacterState.AddState(States.Immateriality, (durationLeap * rangeLeap), 0, _player.gameObject, Name);

        _player.Move.enabled = false;

        playerTransform.TargetRpcDoMove(firstLeapPoint, (durationLeap * rangeLeap / GlobalVariable.cellSize));
    }

    [Command]
    private void CmdExecuteTwoLeaps(Vector2 firstLeapPoint, Vector2 secondLeapPoint,
        float durationLeap, float rangeLeap, float timeBuff, bool heatedGlandsIsActive,
        LayerMask enemyLayer)
    {
        MoveComponent playerTransform = _player.GetComponent<MoveComponent>();

        _player.CharacterState.AddState(States.Immateriality, (durationLeap * rangeLeap) * 1.25f, 0, _player.gameObject, Name);

        _player.Move.enabled = false;

        Sequence leapSequence = DOTween.Sequence();

        leapSequence.AppendCallback(() =>
        {
            playerTransform.TargetRpcDoMove(firstLeapPoint, (durationLeap * rangeLeap / GlobalVariable.cellSize));
        });

        leapSequence.AppendInterval(0.5f);

        leapSequence.AppendCallback(() =>
        {
            if (_isTargetOnEndPointSecondLeap)
            {
                playerTransform.TargetRpcDoMoveSequence(firstLeapPoint, secondLeapPoint, (durationLeap * rangeLeap / GlobalVariable.cellSize),
                    _player, _isTargetBehindPlayer);

                if (heatedGlandsIsActive)
                {
                    _player.CharacterState.AddState(States.HeatedGlands, 4, 0, _player.gameObject, null);
                }
            }
            else
            {
                playerTransform.TargetRpcDoMoveSequence(firstLeapPoint, secondLeapPoint, (durationLeap * rangeLeap / GlobalVariable.cellSize),
                    _player, _isTargetBehindPlayer);
                if (heatedGlandsIsActive)
                {
                    _player.CharacterState.AddState(States.HeatedGlands, 4, 0, _player.gameObject, null);
                }
            }
        });
    }
}
    #endregion