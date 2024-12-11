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
    [SerializeField] private float _invtervalBetweenLeaps;
    [SerializeField] private float _multiplierDistanceInPrecentage = 1.08f;
    [SerializeField] private float PrecentageOfDistance = 0.1f;

    private Character _target;
    private Character _targetForAbility;
    private Dictionary<Character, bool> _targetsCanBeHit = new Dictionary<Character, bool>();

    private Vector3 _firstLeapPoint = Vector3.positiveInfinity;
    private Vector3 _secondLeapPoint;
    private Vector3 _startPosition;
    private Vector3 _firstLeapPointIfIsObstacle;
    private Vector3 _secondLeapPointIfIsObstacle;
    private RaycastHit[] _hitsObstacle;

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
    private Coroutine _timerForEndCastCoroutine;

    #endregion

    private float _animTime;
    private float _baseAnimationMultiplierSpeed = 1f;
    private float _baseRangeLeap;
    private float _multiplierDistanceForSingleLeap;
    private float _multiplierDistanceForTwoLeaps;
    private float _heatedGlandsDuration = 4f;
    private float _radiusChecking = 1.5f;
    private float _baseCooldownTime;
    private float _reducingCooldownMultiplier = 2f;

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
    private bool _cooldownHasChange;

    #endregion

    public float RadiusAttacks => _radiusAttack;
    public float DurationLeap => _durationLeap;
    public bool IsInMovement { get => _isInMovement; }
    public Character Target { get => _targetForAbility; }

    protected override int AnimTriggerCast => Animator.StringToHash("LightningMovementCastAnim");
    protected override int AnimTriggerCastDelay => 0;
    protected override bool IsCanCast => CheckCanCast();

    #endregion

    #region PrepareAndStartJob

    protected override void Awake()
    {
        base.Awake();

        _baseCooldownTime = _cooldownTime;
    }

    public void AnimLightningMovementCast()
    {
        AnimStartCastCoroutine();
    }

    public void AnimLightningMovementCastEnd()
    {
        _player.Animator.SetTrigger("LightningMovementEndCast");
        AnimCastEnded();
    }

    protected override void ClearData()
    {
        _target = null;

        _player.Move.enabled = true;

        _isFirstClickDone = false;
        _isSecondClickDone = false;
        _isTarget = false;

        _firstLeapPoint = Vector3.positiveInfinity;
        _secondLeapPoint = Vector3.zero;
        _startPosition = Vector3.zero;
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
            Invoke("ResetBools", timer);
        }
    }

    protected override IEnumerator PrepareJob()
    {
        if (_cooldownHasChange)
            ReturnCooldown();

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

        SetAnimMultiplierSpeed();
        _player.Animator.applyRootMotion = true;

        AnimLightningMovementCast();
    }

    protected override IEnumerator CastJob()
    {
        if (_isFirstClickDone && !_isTarget)
        {
            if (IsObstacle(transform.position, _firstLeapPoint))
            {
                _firstLeapPoint = NewPointForLeap(_firstLeapPoint);
            }
            Debug.Log("SingleLeap / IsObstacle? = " + IsObstacle(transform.position, _firstLeapPoint));

            SingleLeap(_firstLeapPoint);
        }
        else if (_isFirstClickDone && _isSecondClickDone && _isTarget)
        {
            _applyDamageCoroutine = StartCoroutine(ApplyDamageJob(_targetsLayers, _radiusAttack, _durationLeap * _rangeLeap));

            if (IsObstacle(transform.position, _firstLeapPoint))
            {
                Debug.Log("TwoLeaps / IsObstacle? = " + IsObstacle(transform.position, _firstLeapPoint));
                _firstLeapPoint = NewPointForLeap(_firstLeapPoint);
                SingleLeap(_firstLeapPoint);
            }
            else
            {
                if (IsObstacle(_firstLeapPoint, _secondLeapPoint))
                {
                    Debug.Log("TwoLeaps / IsObstacle? = " + IsObstacle(transform.position, _firstLeapPoint));
                    _secondLeapPoint = NewPointForLeap(_secondLeapPoint);
                }

                ExecuteLeaps(_firstLeapPoint, _secondLeapPoint);
            }
        }

        yield return null;
    }

    private void ResetBools()
    {
        Debug.Log("ResetBools");
        _targetsCanBeHit.Clear();
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

    private void ReducingCooldown()
    {
        _cooldownHasChange = true;

        _cooldownTime /= _reducingCooldownMultiplier;
    }

    private void ReturnCooldown()
    {
        _cooldownHasChange = false;

        _cooldownTime = _baseCooldownTime;
    }

    private void SetAnimMultiplierSpeed()
    {
        _animTime = GetAnimationClipLength();

        if (_animTime > 0)
        {
            float multiplierSpeed = _durationLeap;
            float animMultiplierSpeed = _animTime / multiplierSpeed;

            _player.Animator.SetFloat("LightningMovementMultiplierSpeedAnimation", animMultiplierSpeed);
        }
    }

    private float GetAnimationClipLength()
    {
        RuntimeAnimatorController animController = _player.Animator.runtimeAnimatorController;
        foreach (var clip in animController.animationClips)
        {
            if (clip.name == "LightningMovementAnimation")
            {
                return clip.length;
            }
        }
        return -1f;
    }

    #endregion

    #region CheckMethods

    private Vector3 LimitFirstLeapToMaxDistance(Vector3 startPoint, Vector3 targetPoint, float maxDistance)
    {
        Vector3 direction = (targetPoint - startPoint).normalized;
        Vector3 newPoint = startPoint + (direction * maxDistance);
        newPoint.y = 0;

        _multiplierDistanceForSingleLeap = (maxDistance * _durationLeap) * PrecentageOfDistance;

        return newPoint;
    }

    private Vector3 LimitSecondLeapToMaxDistance(Vector3 startPoint, Vector3 targetPoint, float maxDistance)
    {
        Vector3 centerTarget = Vector3.zero;
        Vector3 newPoint;

        Vector3 direction = (targetPoint - startPoint).normalized;

        if (_isTargetOnEndPointCoroutine == null)
        {
            _isTargetOnEndPointCoroutine = StartCoroutine(IsTargetOnEndPoint());
        }

        if (_isTargetOnEndPointSecondLeap)
        {
            if (_target != null)
            {
                centerTarget = _target.GetComponent<Character>().Collider.bounds.center;
            }

            bool isPointBehindCenterTarget = Vector3.Distance(startPoint, targetPoint) > Vector3.Distance(startPoint, centerTarget);

            if (isPointBehindCenterTarget)
            {
                maxDistance *= _multiplierDistanceInPrecentage;
            }
            else
            {
                maxDistance /= _multiplierDistanceInPrecentage;
            }

            _multiplierDistanceForTwoLeaps = maxDistance * PrecentageOfDistance;

            newPoint = startPoint + (direction * maxDistance);
            newPoint.y = 0;

            return newPoint;
        }
        else
        {
           _multiplierDistanceForTwoLeaps = maxDistance * PrecentageOfDistance;

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
        Debug.Log("IsObstacle()");

        Vector3 direction = (endPos - startPos).normalized;

        RaycastHit[] hitObstacle = Physics.RaycastAll(startPos, direction, _castLength, _obstacle);
        Debug.Log("IsObstacle() / hitObstacle.Lenght = " + hitObstacle.Length);
        if (hitObstacle.Length > 0)
        {
            _hitsObstacle = hitObstacle;
            return true;
        }
        
        return false;
    }

    private Vector3 NewPointForLeap(Vector3 startPoint)
    {
        Debug.Log("NewPointForLeap()");
        Vector3 newPoint = startPoint;
        if (_hitsObstacle.Length > 0)
        {
            foreach (RaycastHit hit in _hitsObstacle)
            {
                if (hit.collider != null)
                {
                    newPoint = hit.point;
                    newPoint.y = 0;
                }

                return newPoint;
            }
        }

        return newPoint;
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
        while (_isAbilityDone == false)
        {
            Collider[] enemies = Physics.OverlapSphere(_firstLeapPoint, _radiusChecking, _targetsLayers);
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

                _startPosition = _player.transform.position;
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

                if (Vector3.Distance(rawSecondLeapPoint, _startPosition) <= 1f)
                {
                    ReducingCooldown();
                }

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
        float minTimeCooldown = 0.2f;

        while (true)
        {
            Collider[] hits = Physics.OverlapSphere(_player.transform.position, radius, enemiesLayer);

            foreach (Collider item in hits)
            {
                if (item != null)
                {
                    bool targetCanBeHit = true;

                    Character targetCharacter = item.gameObject.GetComponent<Character>();
                    _targetForAbility = targetCharacter;

                    if (_targetsCanBeHit.ContainsKey(targetCharacter) == false)
                        _targetsCanBeHit.Add(targetCharacter, targetCanBeHit);

                    if (_targetsCanBeHit[targetCharacter] == true)
                    {
                        if (_lightningStrikes.IsCanDamageDeal)
                        {
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

                        targetCanBeHit = false;

                        _targetsCanBeHit[targetCharacter] = targetCanBeHit;
                    }
                }
            }

            yield return null;
        }
    }

    private IEnumerator TimerForEndCast(float time)
    {
        while (time > 0)
        {
            time -= Time.deltaTime;
            yield return null;
        }

        AnimLightningMovementCastEnd();

        if (_animTime > 0)
            _player.Animator.SetFloat("LightningMovementMultiplierSpeedAnimation", _baseAnimationMultiplierSpeed);

        _player.Animator.applyRootMotion = false;
        StopCoroutine(_timerForEndCastCoroutine);
        _timerForEndCastCoroutine = null;
    }

    #endregion

    #region Leaps

    private void SingleLeap(Vector3 firstLeapPoint)
    {
        _isAbilityDone = true;
        _isInMovement = true;

        _player.Move.enabled = false;

        CmdSingleLeap(firstLeapPoint, _durationLeap, _rangeLeap, _multiplierDistanceForSingleLeap, _heatedGlandsDuration, _heatedGlandsIsActive);
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
                _durationLeap, _rangeLeap, _multiplierDistanceForTwoLeaps, _invtervalBetweenLeaps, _heatedGlandsDuration,
                _heatedGlandsIsActive, _targetsLayers, _target.gameObject);
        }

    }

    #endregion

    #region Command

    [Command]
    private void CmdSingleLeap(Vector3 firstLeapPoint, float durationLeap, float rangeLeap, float multiplierDistanceLeap, float heatedGlandsDuration,
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

        TargetRpcSingleLeap(firstLeapPoint, _player.gameObject, durationLeap, rangeLeap, multiplierDistanceLeap);
    }

    [Command]
    private void CmdExecuteTwoLeaps(Vector3 firstLeapPoint, Vector3 secondLeapPoint,
        float durationLeap, float rangeLeap, float multiplierDistanceLeap, float interval, float heatedGlandsDuration,
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
            durationLeap, rangeLeap, multiplierDistanceLeap, interval, heatedGlandsDuration,
            heatedGlandsIsActive);
    }

    #endregion

    #region RPCMethods

    [TargetRpc]
    private void TargetRpcSingleLeap(Vector3 firstLeapPoint, GameObject player,
        float durationLeap, float rangeLeap, float multiplierDistanceLeap)
    {
        _player.CharacterState.CmdAddState(States.Immateriality, _durationLeap - multiplierDistanceLeap, 0, _player.gameObject, Name);

        Character playerCharacter = player.GetComponent<Character>();

        Vector3 direction = (firstLeapPoint - playerCharacter.transform.position).normalized;
        direction.y = 0;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        _player.Animator.rootRotation = targetRotation;

        playerCharacter.Rb.DOMove(firstLeapPoint, durationLeap * rangeLeap / GlobalVariable.cellSize).SetEase(Ease.OutSine);

        if (_timerForEndCastCoroutine == null)
            _timerForEndCastCoroutine = StartCoroutine(TimerForEndCast(durationLeap + multiplierDistanceLeap));
    }

    [TargetRpc]
    private void TargetRpcExecuteLeaps(GameObject player, Vector3 firstLeapPoint, Vector3 secondLeapPoint,
        float durationLeap, float rangeLeap, float multiplierDistanceLeap, float interval, float heatedGlandsDuration,
        bool heatedGlandsIsActive)
    {
        Character playerCharacter = player.GetComponent<Character>();
        Animator playerAnimator = player.GetComponent<Animator>();

        Sequence leapSequence = DOTween.Sequence();

        playerCharacter.CharacterState.CmdAddState(States.Immateriality, _durationLeap * rangeLeap, 0, _player.gameObject, Name);

        interval = (rangeLeap * (durationLeap / GlobalVariable.cellSize));

        leapSequence.AppendCallback(() =>
        {
            if (heatedGlandsIsActive)
            {
                playerCharacter.CharacterState.CmdAddState(States.HeatedGlands, heatedGlandsDuration, 0, _player.gameObject, null);
            }

            playerCharacter.transform.LookAt(firstLeapPoint);

            playerCharacter.Rb.DOMove(firstLeapPoint, (durationLeap * rangeLeap / GlobalVariable.cellSize)).SetEase(Ease.OutSine);
            
        }).AppendInterval(interval);

        leapSequence.AppendCallback(() =>
        {
            if (heatedGlandsIsActive)
            {
                playerCharacter.CharacterState.CmdAddState(States.HeatedGlands, heatedGlandsDuration, 0, _player.gameObject, null);
            }

            playerCharacter.transform.LookAt(secondLeapPoint);

            playerCharacter.Rb.DOMove(secondLeapPoint, (durationLeap * rangeLeap / GlobalVariable.cellSize)).SetEase(Ease.OutSine);

            if (_timerForEndCastCoroutine == null)
                _timerForEndCastCoroutine = StartCoroutine(TimerForEndCast(durationLeap + multiplierDistanceLeap));
        });
    }

    #endregion
}
