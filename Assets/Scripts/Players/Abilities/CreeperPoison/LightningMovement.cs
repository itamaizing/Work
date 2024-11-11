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

    private float _baseRangeLeap;
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

    protected override int AnimTriggerCast => 0;
    protected override int AnimTriggerCastDelay => 0;
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
        _rangeLeap = _baseRangeLeap;

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
        _baseRangeLeap = _rangeLeap;

        StopAutoDraw();
        _renderLineForFirstLeapCoroutine = StartCoroutine(RenderLineForFirstLeapJob(_castLength, _castWidth, _line, transform));

        if (_heatedGlands.Data.IsOpen)
        {
            _heatedGlandsIsActive = _heatedGlands.Data.IsOpen;
        }

        while (_target == null && float.IsPositiveInfinity(_firstLeapPoint.z))
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

        bool isPointBehindCenterTarget = Vector3.Distance(startPoint, targetPoint) > Vector3.Distance(startPoint, centerTarget);

        if (_isTargetOnEndPointSecondLeap)
        {
            if (isPointBehindCenterTarget)
            {// Точка прыжка за серединой врага
                maxDistance += 1.5f;
            }
            else
            {// Точка прыжка перед серединой врага
                maxDistance += 2.8f;
            }
            _multiplierLeap = (maxDistance / dividerForMultiplier) + coefficientForMultiplier;
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
        if (_isFirstClickDone && !_isTarget)
        {
            return Vector3.Distance(transform.position, _firstLeapPoint) <= Radius && NoObstacles(_firstLeapPoint, _obstacle);
        }

        return Vector3.Distance(transform.position, _firstLeapPoint) <= Radius && NoObstacles(_firstLeapPoint, _obstacle) ||
               Vector3.Distance(transform.position, _secondLeapPoint) <= Radius && NoObstacles(_secondLeapPoint, _obstacle);
    }

    private bool IsObstacle(Vector3 startPos, Vector3 endPos)
    {
        Vector3 direction = (startPos - endPos).normalized;

        RaycastHit[] hitObstacle = Physics.RaycastAll(startPos, direction, _rangeLeap, _obstacle);
        foreach(RaycastHit hit in hitObstacle)
        {
            if (hit.collider != null)
            {
                _firstLeapPointIfIsObstacle = hit.point;
                _secondLeapPointIfIsObstacle = hit.point;
            }

            Debug.Log("LightningMovement / IsObstacle return true");
            return true;
        }
        Debug.Log("LightningMovement / IsObstacle return false");
        return false;
    }

    private void IsEnemyBeforePlayer()
    {
        float castLengthMultiplier = 4f;
        float castWidthMultiplier = 1.55f;

        Vector3 sizeBox = new Vector3(_castLength * castLengthMultiplier, 1f, _castWidth * castWidthMultiplier);

        /*  #region LookRotation

          Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
          if (Physics.Raycast(ray, out RaycastHit hit))
          {
              Vector3 direction = 
          }

          #endregion */

        Vector3 dir = transform.forward;
        Quaternion lookRotation = Quaternion.LookRotation(dir);


        Debug.Log("LightningMovement / IsEnemyBeforePlayer / lookRotation = " + lookRotation);

        Collider[] hit = Physics.OverlapBox(transform.position, sizeBox, lookRotation, _targetsLayers);
        if (hit.Length > 0)
        {
            Debug.Log("LightningMovement / IsEnemyBeforePlayer / hit = " + hit.Length);
            _isTarget = true;
            Debug.Log("LightningMovement / IsEnemyBeforePlayer / isTarget = true");
        }
        else
        {
            _isTarget = false;
            Debug.Log("LightningMovement / IsEnemyBeforePlayer / isTarget = false");
        }
    }

    private IEnumerator IsTargetOnEndPoint(Vector3 secondLeap, LayerMask targetLayer)
    {
        Debug.Log("LightningMovement / IsTargetOnEndPoint");

        float radiusChecking = 1.35f;
        Vector3 direction = (secondLeap - transform.position).normalized;

        while (true)
        {
            Ray ray = new Ray(transform.position, direction);
            RaycastHit hitInfo;

            bool isHit = Physics.SphereCast(ray, radiusChecking, out hitInfo, targetLayer);
            Debug.Log("LightningMovement / IsTargetOnEndPoint / isHit = " + isHit);
            Debug.Log("LightningMovement / IsTargetOnEndPoint / hitInfo = " + hitInfo.collider.name);
 
            if (isHit)
            {
                _target = hitInfo.collider.gameObject.GetComponent<Character>();

                _isTargetOnEndPointSecondLeap = true;

                Debug.Log("LightningMovement / IsTargetOnEndPoint / if (isHit) / _target = " + _target + " / _isTargetOnEndPointSecondLeap = " + _isTargetOnEndPointSecondLeap);

                yield break;
            }
            yield return null;
        }
    }

    private IEnumerator IsTargetBeforePlayerJob(float rangeLeap, LayerMask targetLayer)
    {
        Vector3 sizeBox = new Vector3((_castLength * 1.5f) / 2, 1f / 2, (_castWidth * 1.3f) / 2);

        Vector3 dir = transform.forward;
        Quaternion lookRotation = Quaternion.LookRotation(dir);
        Debug.Log("LightningMovement / IsTargetBeforePlayerJob (Coroutine) / lookRotation = " + lookRotation);

        while (true)
        {
            Collider[] hitsForward = Physics.OverlapBox(transform.position, sizeBox, lookRotation, targetLayer);
            if (hitsForward.Length > 0)
            {
                _isTargetBeforePlayer = true;
                Debug.Log("LightningMovement / IsTargetBeforePlayerJob (Coroutine) / if (hit != null) / isTargetBeforePlayer = " + _isTargetBeforePlayer);

            }

            yield return null;
        }
    }

    private IEnumerator IsTargetBehindPlayerJob(float rangeLeap, LayerMask targetLayer)
    {
        Vector3 sizeBox = new Vector3((_castLength - 1.5f) / 2, 1f / 2, (_castWidth * 1.2f));

        Vector3 dir = -transform.forward;
        Quaternion lookRotation = Quaternion.LookRotation(dir);
        Debug.Log("LightningMovement / IsTargetBehindPlayerJob (Coroutine) / lookRotation = " + lookRotation);
        while (true)
        {
            Collider[] hit = Physics.OverlapBox(transform.position, sizeBox, lookRotation, targetLayer);
            if (hit.Length > 0)
            {
                _isTargetBehindPlayer = true;
                Debug.Log("LightningMovement / IsTargetBehindPlayerJob (Coroutine) / if (hit != null) / _isTargetBehindPlayer = " + _isTargetBehindPlayer);
            }

            yield return null;
        }
    }

    #endregion

    #region RenderingLine

    private Quaternion DirectionBox(Transform transform)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 direction = hit.point - transform.position;
            direction.y = 0;

            Quaternion lookRotation = Quaternion.LookRotation(direction);
            return lookRotation;
        }

        return Quaternion.identity;
    }

    private Quaternion RotateAtMouse(Transform transform)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 dir = hit.point - transform.position;
            dir.y = 0;

            Quaternion lookRotation = Quaternion.LookRotation(dir);         
            Quaternion tiltRotation = Quaternion.Euler(90f, 0f, 0f);

            transform.rotation = lookRotation * tiltRotation;
        }

        return transform.rotation;
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
            };

            _lineStartImageForFirstLeap.SetSize(width, length / 2, damage);
            _lineEndImageForFirstLeap.SetSize(width, length / 2, damage);
            Debug.Log("LightningMovement / RenderLineFirstVector");
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
            };

            _lineStartImageForSecondLeap.SetSize(width, length / 2, damage);
            _lineEndImageForSecondLeap.SetSize(width, length / 2, damage);
            Debug.Log("LightningMovement / RenderLineSecondVector");
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
        {
            Destroy(_lineStartImageForFirstLeap.gameObject);
        }
        if (_lineEndImageForFirstLeap != null)
        {
            Destroy(_lineEndImageForFirstLeap.gameObject);
        }
        if (_lineStartImageForSecondLeap != null)
        {
            Destroy(_lineStartImageForSecondLeap.gameObject);
        }
        if (_lineEndImageForSecondLeap != null)
        {
            Destroy(_lineEndImageForSecondLeap.gameObject);
        }
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
                _firstLeapPoint.y = _player.transform.position.y;
                Debug.Log("LightningMovement / FirstPoint = " + _firstLeapPoint);
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
                _secondLeapPoint.y = _player.transform.position.y;

                Debug.Log("LightningMovement / SecondPoint = " + _secondLeapPoint);
            }
            yield return null;
        }
    }

    private IEnumerator MidpointForRenderingSecondLeap(Vector3 firstLeapPoint)
    {
        Debug.Log("LightningMovement / MidpointForRenderingSecondLeap");
        Vector3 originalPoint = firstLeapPoint;

        _midPointForLeap = Instantiate(_midPointForLeapPrefab, originalPoint, Quaternion.identity);

        _renderLineForSecondLeapCoroutine = StartCoroutine(RenderLineForSecondLeapJob(_castLength, _castWidth, _line, _midPointForLeap.transform));

        yield return _secondPointForLeapCoroutine = StartCoroutine(SecondVectorForLeap());

        Destroy(_midPointForLeap);

        StopRenderLine();
    }

    private IEnumerator ApplyDamageJob(LayerMask enemiesLayer, float radius, float duration)
    {
        while (true)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, radius, enemiesLayer);

            foreach (Collider item in hits)
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

                    bool isAtFirstLeapEnd = Vector2.Distance(_player.transform.position, _firstLeapPoint) < 0.1f;
                    bool isAtSecondLeapStart = Vector2.Distance(_player.transform.position, _secondLeapPoint) < 0.1f;

                    if (!isAtFirstLeapEnd && !isAtSecondLeapStart)
                    {
                        if (_lightningStrikes.IsCanDamageDeal)
                        {
                            _lightningStrikes.UseLightningStrikesOfLightningMovement(targetCharacter, duration);
                        }
                        else if (_poisonSlap.IsCanDamageDeal && _poisonSlap.RemainingCooldownTime <= 0)
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

    #region EvadeLogic

    private void OnEnabled()
    {
        Debug.Log("LightningMovement / OnEnabled");
        _player.Health.Evaded += EvadeMagDamage;
    }

    private void OnDisabled()
    {
        _player.Health.Evaded -= EvadeMagDamage;
    }

    private void EvadeMagDamage()
    {
        Damage damage = new Damage
        {
            Value = 0f,
        };
        _player.Health.TryTakeDamage(ref damage, null);
    }

    #endregion

    #region Leaps
    private void SingleLeap(Vector3 firstLeapPoint)
    {
        _isAbilityDone = true;
        IsInMovement = true;

        _player.Move.enabled = false;

        if (_isTargetBeforePlayerCoroutine == null)
        {
            _isTargetBeforePlayerCoroutine = StartCoroutine(IsTargetBeforePlayerJob(_rangeLeap, _targetsLayers));
        }

        _player.CharacterState.CmdAddState(States.Immateriality, _durationLeap, 0, _player.gameObject, Name);

        CmdSingleLeap(firstLeapPoint, _durationLeap, _rangeLeap, _multiplierLeap, _timeBuff, _heatedGlandsIsActive);
    }

    private void ExecuteLeaps(Vector3 firstLeapPoint, Vector3 secondLeapPoint)
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

        _applyDamageCoroutine = StartCoroutine(ApplyDamageJob(_targetsLayers, _radiusAttack, _durationLeap * _rangeLeap));

        _player.CharacterState.CmdAddState(States.Immateriality, (_durationLeap * _multiplierLeap), 0, _player.gameObject, Name);

        CmdExecuteTwoLeaps(firstLeapPoint, secondLeapPoint, 
            _durationLeap, _rangeLeap, _multiplierLeap, _timeBuff, _invtervalBetweenLeaps,
            _heatedGlandsIsActive, _targetsLayers, _target.gameObject);
    }

    #endregion

    #region Command

    [Command]
    private void CmdSingleLeap(Vector3 firstLeapPoint, 
        float durationLeap, float rangeLeap, float multiplierLeap, float timeBuff, 
        bool heatedGlandsIsAcitve)
    {

        _player.Move.enabled = false;

        if (_superFastScales.Data.IsOpen)
        {
            OnEnabled();
            _superFastScales.IncreasingResistance(null);
        }

        MoveComponent playerTransform = _player.GetComponent<MoveComponent>();
        
        playerTransform.TargetRpcDoMove(firstLeapPoint, (durationLeap * rangeLeap / GlobalVariable.cellSize));
    }

    [Command]
    private void CmdExecuteTwoLeaps(Vector3 firstLeapPoint, Vector2 secondLeapPoint,
        float durationLeap, float rangeLeap, float multiplierLeap, float timeBuff, float interval,
        bool heatedGlandsIsActive,
        LayerMask enemyLayer, GameObject target)
    {
        Character targetCharacter = target.GetComponent<Character>();
        _player.Move.enabled = false;

        if (_superFastScales.Data.IsOpen)
        {
            OnEnabled();
            _superFastScales.IncreasingResistance(targetCharacter);
        }

        TargetRpcExecuteLeaps(_player.gameObject, firstLeapPoint, secondLeapPoint, 
            durationLeap, rangeLeap, multiplierLeap, timeBuff, interval,
            heatedGlandsIsActive, enemyLayer);
    }

    #endregion

    [TargetRpc]
    private void TargetRpcExecuteLeaps(GameObject player, Vector3 firstLeapPoint, Vector3 secondLeapPoint,
        float durationLeap, float rangeLeap, float multiplierLeap, float timeBuff, float interval,
        bool heatedGlandsIsActive,
        LayerMask enemyLayer)
    {
        float deductible = 0.12f;
        interval = (rangeLeap * (durationLeap / GlobalVariable.cellSize)) - deductible;
        Character playerRigidbody = player.GetComponent<Character>(); 

        Sequence leapSequence = DOTween.Sequence();

        leapSequence.AppendCallback(() =>
        {
            playerRigidbody.Rb.DOMove(firstLeapPoint, (durationLeap * rangeLeap / GlobalVariable.cellSize));
        }).AppendInterval(interval);

        leapSequence.AppendCallback(() =>
        {
            playerRigidbody.Rb.DOMove(secondLeapPoint, (durationLeap * rangeLeap / GlobalVariable.cellSize));
            if (heatedGlandsIsActive)
            {
                _player.CharacterState.AddState(States.HeatedGlands, 4, 0, _player.gameObject, null);
            }
        });
    }
}
