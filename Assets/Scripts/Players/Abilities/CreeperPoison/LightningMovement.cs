using DG.Tweening;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightningMovement : Skill
{
    #region Variables

    [Header("Talents")]
    [SerializeField] private SuperFastScales _superFastScales;
    [SerializeField] private HeatedGlands _heatedGlands;
    [SerializeField] private LightningFastPoisonSlap _lightningFastPoisonSlap;
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

    private Character _target;
    private Character _targetForApplyDamage;
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
    private GameObject _pointForRenderFirstVector;

    #endregion

    #region CoroutineVariables

    private Coroutine _firstPointForLeapCoroutine;
    private Coroutine _secondPointForLeapCoroutine;
    private Coroutine _midPointForLeapCoroutine;
    private Coroutine _renderLineForFirstLeapCoroutine;
    private Coroutine _renderLineForSecondLeapCoroutine;
    private Coroutine _isTargetBeforePlayerCoroutine;
    private Coroutine _isTargetOnEndPointCoroutine;
    private Coroutine _applyDamageCoroutine;

    #endregion

    private float _baseRangeLeap;
    private float _multiplierLeap = 1f;
    private float _heatedGlandsDuration = 4f;

    #region BoolVariables

    [SyncVar] private bool _isTargetBeforePlayer = false;
    [SyncVar] private bool _isTargetBehindPlayer = false;
    [SyncVar] private bool _isTargetOnEndPointSecondLeap = false;
    private bool _isFirstClickDone = false;
    private bool _isSecondClickDone = false;
    private bool _isTarget = false;
    private bool _isAbilityDone = false;
    private bool _heatedGlandsIsActive;
    private bool _isInMovement = false;

    #endregion

    public float RadiusAttacks => _radiusAttack;
    public float DurationLeap => _durationLeap;
    public bool IsInMovement { get => _isInMovement; }
    public Character Target { get => _targetForApplyDamage; }
    protected override int AnimTriggerCast => 0;
    protected override int AnimTriggerCastDelay => 0;
    protected override bool IsCanCast => CheckCanCast();

    #endregion

    #region PrepareAndStartJob

    protected override void ClearData()
    {
        _target = null;

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

        if (_midPointForLeap != null)
        {
            Destroy(_midPointForLeap.gameObject);
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

        _isInMovement = false;
        _lightningStrikes.IsCanDamageDeal = false;

        if (_isTargetBeforePlayerCoroutine != null)
        {
            StopCoroutine(_isTargetBeforePlayerCoroutine);
            _isTargetBeforePlayerCoroutine = null;
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
        _renderLineForFirstLeapCoroutine = StartCoroutine(RenderLineForFirstLeapJob(_castLength, _castWidth, _line));

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
            _applyDamageCoroutine = StartCoroutine(ApplyDamageJob(_targetsLayers, _radiusAttack, _durationLeap * _rangeLeap));

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
        Vector3 newPoint = startPoint + (direction * maxDistance);
        newPoint.y = 0;

        return newPoint;
    }

    private Vector3 LimitSecondLeapToMaxDistance(Vector3 startPoint, Vector3 targetPoint, float maxDistance)
    {
        Vector3 centerTarget;
        Vector3 newPoint;

        Vector3 direction = (targetPoint - startPoint).normalized;

        float dividerForMultiplier = 10f;
        float coefficientForMultiplier = 0.5f;

        if (_isTargetOnEndPointCoroutine == null)
        {
            _isTargetOnEndPointCoroutine = StartCoroutine(IsTargetOnEndPoint());
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

            newPoint = startPoint + (direction * maxDistance);
            newPoint.y = 0;

            return newPoint;
        }
        else
        {
            _multiplierLeap = (maxDistance / dividerForMultiplier) + coefficientForMultiplier;

            newPoint = startPoint + (direction * maxDistance);
            newPoint.y = 0;

            return newPoint;
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

            return true;
        }
        return false;
    }

    #region CheckTarget

    private void IsEnemyBeforePlayer()
    {
        Vector3 sizeBox = new Vector3(_castWidth / 2, 1f / 2, _castLength / 2);
        Vector3 forwardPosition = _player.transform.position + transform.forward / 1.5f;

        Collider[] hits = Physics.OverlapBox(forwardPosition, sizeBox, transform.rotation, _targetsLayers);
        if (hits.Length > 0)
        {
            foreach (Collider collider in hits)
            {
                _target = collider.gameObject.GetComponent<Character>();
            }
            _isTarget = true;
        }
        else
        {
            _isTarget = false;
        }
    }

    private IEnumerator IsTargetOnEndPoint()
    {
        while (true)
        {
            Collider[] enemies = Physics.OverlapSphere(_firstLeapPoint, _radiusAttack, _targetsLayers);
            if (enemies.Length > 0)
            {
                foreach (Collider target in enemies)
                {
                    if (target != null)
                    {
                        _target = target.GetComponent<Character>();
                        _isTargetOnEndPointSecondLeap = true;
                    }
                }
            }
            yield return null;
        }
    }

    private IEnumerator IsTargetBeforePlayerJob()
    {
        Vector3 sizeBox = new Vector3(_castWidth / 2, 1f / 2, _castLength / 2);
        Vector3 forwardPosition = _player.transform.position + transform.forward / 1.5f;

        while (true)
        {
            Collider[] hitsForward = Physics.OverlapBox(forwardPosition, sizeBox, transform.rotation, _targetsLayers);
            if (hitsForward.Length > 0)
            {
                _isTargetBeforePlayer = true;
            }

            yield return null;
        }
    }

    #endregion

    #endregion

    #region RenderingLine

    private Quaternion RotateAtMouse(Transform transformLine)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 dir = hit.point - transformLine.position;
            dir.y = 0;

            Quaternion lookRotation = Quaternion.LookRotation(dir);         
            Quaternion tiltRotation = Quaternion.Euler(90f, 0f, 0f);

            transformLine.rotation = lookRotation * tiltRotation;
        }

        return transformLine.rotation;
    }

    private IEnumerator RenderLineForFirstLeapJob(float length, float width, AbilityLineRenderer line)
    {
        _pointForRenderFirstVector = Instantiate(_midPointForLeapPrefab, _player.transform);

        _lineStartImageForFirstLeap = Instantiate(line.Start, _pointForRenderFirstVector.transform);
        _lineEndImageForFirstLeap = Instantiate(line.End, _pointForRenderFirstVector.transform);

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
            yield return null;
        }

        _lineStartImageForFirstLeap.transform.SetParent(null);
        _lineEndImageForFirstLeap.transform.SetParent(null);
        _pointForRenderFirstVector.transform.SetParent(null);

        _lineStartImageForFirstLeap.transform.position = _lineStartImageForFirstLeap.transform.position;
        _lineEndImageForFirstLeap.transform.position = _lineEndImageForFirstLeap.transform.position;
        _pointForRenderFirstVector.transform.position = _pointForRenderFirstVector.transform.position;
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
            yield return null;
        }
    }

    private void StopRenderLine()
    {
        if (_pointForRenderFirstVector != null)
        {
            Destroy(_pointForRenderFirstVector);
        }

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
                _firstLeapPoint.y = 0;
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
                _secondLeapPoint.y = 0;
            }
            yield return null;
        }
    }

    private IEnumerator MidpointForRenderingSecondLeap(Vector3 firstLeapPoint)
    {
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
            Collider[] hits = Physics.OverlapSphere(_player.transform.position, radius, enemiesLayer);

            foreach (Collider item in hits)
            {
                if (item != null)
                {
                    GameObject target = item.gameObject;
                    var targetCharacter = item.gameObject.GetComponent<Character>();

                    _targetForApplyDamage = targetCharacter;

                    if (_targetHitTimes.ContainsKey(target))
                    {
                        if (Time.time - _targetHitTimes[target] < _cooldownAttack)
                        {
                            continue;
                        }
                    }

                    bool isAtFirstLeapEnd = Vector3.Distance(_player.transform.position, _firstLeapPoint) < 0.1f;
                    bool isAtSecondLeapStart = Vector3.Distance(_player.transform.position, _secondLeapPoint) < 0.1f;

                    if (!isAtFirstLeapEnd && !isAtSecondLeapStart)
                    {
                        float minTimeCooldown = 0.2f;

                        if (_lightningStrikes.IsCanDamageDeal)
                        {
                            Debug.Log("LightningMovement / ApplyDamage / if lightningStrike / isCanDamageDeal = " + _lightningStrikes.IsCanDamageDeal);

                           _lightningStrikes.UseLightningStrikesOfLightningMovement();
                        }
                        else if (_lightningFastPoisonSlap.Data.IsOpen && _poisonSlap.IsCanDamageDeal && _poisonSlap.RemainingCooldownTime <= minTimeCooldown)
                        {
                            _poisonSlap.UsePoisonSlapOfLightningMovement();
                        }
                        else
                        {
                           _creeperStrike.DamageDeal(targetCharacter);
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

    private void SingleLeap(Vector3 firstLeapPoint)
    {
        _isAbilityDone = true;
        _isInMovement = true;

        _player.Move.enabled = false;

        CmdSingleLeap(firstLeapPoint, _durationLeap, _rangeLeap, _heatedGlandsDuration, _heatedGlandsIsActive);
    }

    private void ExecuteLeaps(Vector3 firstLeapPoint, Vector3 secondLeapPoint)
    {
        if (_target != null)
        {
            _isAbilityDone = true;
            _isInMovement = true;
            _player.Move.enabled = false;

            if (_isTargetBeforePlayerCoroutine == null)
            {
                _isTargetBeforePlayerCoroutine = StartCoroutine(IsTargetBeforePlayerJob());
            }

            CmdExecuteTwoLeaps(firstLeapPoint, secondLeapPoint,
                _durationLeap, _rangeLeap, _multiplierLeap, _timeBuff, _invtervalBetweenLeaps, _heatedGlandsDuration,
                _heatedGlandsIsActive, _targetsLayers, _target.gameObject);
        }

    }

    #endregion

    #region Command

    [Command]
    private void CmdSingleLeap(Vector3 firstLeapPoint, float durationLeap, float rangeLeap, float heatedGlandsDuration, 
        bool heatedGlandsIsActive)
    {
        _player.Move.enabled = false;

        if (_superFastScales.Data.IsOpen)
        {
            _superFastScales.IncreasingResistance(null);
        }

        if (heatedGlandsIsActive)
        {
            _player.CharacterState.AddState(States.HeatedGlands, heatedGlandsDuration, 0, _player.gameObject, null);
        }

        MoveComponent playerTransform = _player.GetComponent<MoveComponent>();
        
        _player.CharacterState.AddState(States.Immateriality, _durationLeap, 0, _player.gameObject, Name);

        playerTransform.TargetRpcDoMove(firstLeapPoint, (durationLeap * rangeLeap / GlobalVariable.cellSize));
    }

    [Command]
    private void CmdExecuteTwoLeaps(Vector3 firstLeapPoint, Vector3 secondLeapPoint,
        float durationLeap, float rangeLeap, float multiplierLeap, float timeBuff, float interval, float heatedGlandsDuration,
        bool heatedGlandsIsActive,
        LayerMask enemyLayer, GameObject target)
    {
        Character targetCharacter = target.GetComponent<Character>();
        _player.Move.enabled = false;

        if (_superFastScales.Data.IsOpen)
        {
            _superFastScales.IncreasingResistance(targetCharacter);
        }

        TargetRpcExecuteLeaps(_player.gameObject, firstLeapPoint, secondLeapPoint, 
            durationLeap, rangeLeap, multiplierLeap, timeBuff, interval, heatedGlandsDuration,
            heatedGlandsIsActive, enemyLayer);
    }

    #endregion

    [TargetRpc]
    private void TargetRpcExecuteLeaps(GameObject player, Vector3 firstLeapPoint, Vector3 secondLeapPoint,
        float durationLeap, float rangeLeap, float multiplierLeap, float timeBuff, float interval, float heatedGlandsDuration,
        bool heatedGlandsIsActive,
        LayerMask enemyLayer)
    {
        _player.CharacterState.CmdAddState(States.Immateriality, (_durationLeap * _multiplierLeap), 0, _player.gameObject, Name);

        interval = (rangeLeap * (durationLeap / GlobalVariable.cellSize));

        Character playerRigidbody = player.GetComponent<Character>(); 

        Sequence leapSequence = DOTween.Sequence();

        leapSequence.AppendCallback(() =>
        {
            if (heatedGlandsIsActive)
            {
                _player.CharacterState.CmdAddState(States.HeatedGlands, heatedGlandsDuration, 0, _player.gameObject, null);
            }

            playerRigidbody.Rb.DOMove(firstLeapPoint, (durationLeap * rangeLeap / GlobalVariable.cellSize));

        }).AppendInterval(interval);

        leapSequence.AppendCallback(() =>
        {
            if (heatedGlandsIsActive)
            {
                _player.CharacterState.CmdAddState(States.HeatedGlands, heatedGlandsDuration, 0, _player.gameObject, null);
            }

            playerRigidbody.Rb.DOMove(secondLeapPoint, (durationLeap * rangeLeap / GlobalVariable.cellSize));
        });
    }
}
