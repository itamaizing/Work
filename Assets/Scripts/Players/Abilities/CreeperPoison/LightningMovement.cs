using DG.Tweening;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using Unity.VisualScripting;
using UnityEditor.Playables;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Progress;

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
    [SerializeField] private Collider2D _playerCollider;
    [SerializeField] private Color _colorForEnd;
    [SerializeField] private Color _colorForStart;

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
    private Coroutine _applyDamageCoroutine;

    #endregion

    private float _angle;

    #region BoolVariables

    [SyncVar] private bool _isTargetBeforePlayer = false;
    [SyncVar] private bool _isTargetBehindPlayer = false;
    private bool _isFirstClickDone = false;
    private bool _isSecondClickDone = false;
    private bool _isTarget = false;
    private bool _isAbilityDone = false;
    private bool _isHittingTarget = false;
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
        
        if(_firstPointForLeapCoroutine != null)
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
                _isTarget = IsEnemyBeforePlayer();

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
        //_targetList.Clear();

        _isTargetBeforePlayer = false;
        _isTargetBehindPlayer = false;

        IsInMovement = false;
        _poisonSlap.IsCanDamageDeal = false;
        _lightningStrikes.IsCanDamageDeal = false;

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

        if (_superFastScales.IsActive)
        {
            _superFastScales.ResetResistance();
        }

    }

    #endregion

    #region CheckMethods

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

    private bool IsEnemyBeforePlayer()
    {
        Vector2 sizeBox = new Vector2(_castLength, _castWidth);
        Collider2D hit = Physics2D.OverlapBox(transform.position, sizeBox, _targetsLayers);
        return hit != null;
    }

    private IEnumerator IsEnemyBeforePlayerJob(Vector3 startPos, Vector3 endPos, float rangeLeap, LayerMask enemyLayer)
    {
        Vector2 sizeBox = new Vector2(_castLength, _castWidth);
        while (true)
        {
            Collider2D hit = Physics2D.OverlapBox(transform.position, sizeBox, _angle - 90f, enemyLayer);
            if (hit != null)
            {
                _isTargetBeforePlayer = true;
            }
            
            yield return null;
        }  
    }

    private IEnumerator IsEnemyBehindPlayerJob(Vector3 startPos, Vector3 endPos, float rangeLeap, LayerMask enemyLayer)
    {
        Vector2 sizeBox = new Vector2(_castLength * 1.2f, _castWidth * 1.2f);
        while (true) 
        {
            Collider2D hit = Physics2D.OverlapBox(transform.position, sizeBox, _angle + 90f, enemyLayer);
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
                _isFirstClickDone = true;
                _firstLeapPoint = GetMousePoint();
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
                _isSecondClickDone = true;
                _secondLeapPoint = GetMousePoint();
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

    private IEnumerator ApplyDamage(LayerMask enemiesLayer, float radius)
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
            _isTargetBeforePlayerCoroutine = StartCoroutine(IsEnemyBeforePlayerJob(_player.transform.position, firstLeapPoint, rangeLeap, _targetsLayers));
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
            _isTargetBeforePlayerCoroutine = StartCoroutine(IsEnemyBeforePlayerJob(_player.transform.position, firstLeapPoint, rangeLeap, _targetsLayers));
        }
        if (_isTargetBehindPlayerCoroutine == null)
        {
            _isTargetBehindPlayerCoroutine = StartCoroutine(IsEnemyBehindPlayerJob(_player.transform.position, firstLeapPoint, rangeLeap, _targetsLayers));
        }

        if (_superFastScales.IsActive)
        {
            _superFastScales.IncreasingResistance();
        }

        _applyDamageCoroutine = StartCoroutine(ApplyDamage(_targetsLayers, _radiusAttack));

        CmdExecuteTwoLeaps(firstLeapPoint, secondLeapPoint, durationLeap, rangeLeap, _timeBuff, _heatedGlandsIsActive, _targetsLayers);
    }

    #endregion

    #region Command

    [Command]
    private void CmdSingleLeap(Vector2 firstLeapPoint, float durationLeap, float rangeLeap, float timeBuff, bool heatedGlandsIsAcitve)
    {
        _player.CharacterState.AddState(States.Immateriality, (durationLeap * rangeLeap), 0, _player.gameObject, Name);

        _player.Move.enabled = false;

        _player.Rb.DOMove(firstLeapPoint, durationLeap * rangeLeap / GlobalVariable.cellSize).SetEase(Ease.Linear);
    }

    [Command]
    private void CmdExecuteTwoLeaps(Vector2 firstLeapPoint, Vector2 secondLeapPoint, 
        float durationLeap, float rangeLeap, float timeBuff, bool heatedGlandsIsActive,
        LayerMask enemyLayer)
    {
        _player.CharacterState.AddState(States.Immateriality, (durationLeap * rangeLeap) * 1.25f, 0, _player.gameObject, Name);

        _player.Move.enabled = false;

        DG.Tweening.Sequence leapSequence = DOTween.Sequence();
        leapSequence.Append(_player.Rb.DOMove(firstLeapPoint, durationLeap * rangeLeap / GlobalVariable.cellSize).SetEase(Ease.Linear));

        if (heatedGlandsIsActive)
        {
            _player.CharacterState.AddState(States.HeatedGlands, timeBuff, 0, _player.gameObject, Name);
        }

        leapSequence.AppendInterval(0.5f);

        leapSequence.AppendCallback(() =>
        {
            if (_isTargetBehindPlayer)
            {
                leapSequence.Append(_player.Rb.DOMove(secondLeapPoint, durationLeap * rangeLeap / GlobalVariable.cellSize).SetEase(Ease.Linear));
                if (heatedGlandsIsActive)
                {
                    _player.CharacterState.AddState(States.HeatedGlands, timeBuff, 0, _player.gameObject, Name);
                }
            }
            else
            {
                leapSequence.Kill();
            }
        });

    }


    #endregion
}