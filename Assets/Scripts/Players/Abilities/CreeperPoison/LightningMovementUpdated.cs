using DG.Tweening;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class LightningMovementUpdated : Skill
{
    [SerializeField] private AcceleratedSlap _acceleratedSlap;
    [SerializeField] private LightningMovementTalent _lMTalent;

    [Header("Ability properties")]
    [SerializeField] private AbilityLineRenderer _line;
    [SerializeField] private GameObject _midPointForLeapPrefab;
    [SerializeField] private Character _player;
    [SerializeField] private Collider2D _playerCollider;
    [SerializeField] private Color _colorForEnd;
    [SerializeField] private Color _colorForStart;

    [SerializeField] private float _rangeLeap;
    [SerializeField] private float _durationLeap;

    #region ForRenderingVectors

    private BoxArea _lineStartImageForFirstLeap;
    private BoxArea _lineEndImageForFirstLeap;
    private BoxArea _lineStartImageForSecondLeap;
    private BoxArea _lineEndImageForSecondLeap;
    private GameObject _midPointForLeap;

    #endregion

    private Character _target;

    private Vector3 _firstLeapPoint = Vector3.positiveInfinity;
    private Vector3 _secondLeapPoint;

    private Vector3 _firstLeapPointIfIsObstacle;
    private Vector3 _secondLeapPointIfIsObstacle;

    private Coroutine _firstPointForLeapCoroutine;
    private Coroutine _secondPointForLeapCoroutine;
    private Coroutine _midPointForLeapCoroutine;
    private Coroutine _renderLineForFirstLeapCoroutine;
    private Coroutine _renderLineForSecondLeapCoroutine;
    private Coroutine _isTargetBeforePlayerCoroutine;
    private Coroutine _isTargetBehindPlayerCoroutine;

    [SyncVar] private bool _isTargetBeforePlayer = false;
    [SyncVar] private bool _isTargetBehindPlayer = false;
    private bool _isFirstClickDone = false;
    private bool _isSecondClickDone = false;
    private bool _isTarget = false;

    protected override bool IsCanCast => CheckCanCast();

    #region PrepareAndStartJob

    protected override void ClearData()
    {
        _player.Move.enabled = true;

        _isFirstClickDone = false;
        _isSecondClickDone = false;
        _isTargetBeforePlayer = false;
        _isTargetBehindPlayer = false;
        _isTarget = false;

        _firstLeapPoint = Vector3.positiveInfinity;
        _secondLeapPoint = Vector3.zero;
        _firstLeapPointIfIsObstacle = Vector3.zero;
        _secondLeapPointIfIsObstacle = Vector3.zero;

        StopRenderLine();

        if (_lMTalent.IsActive)
        {
            // _lMTalent.ResetCharacterResistance();
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
    }

    protected override IEnumerator PrepareJob()
    {
        StopAutoDraw();
        _renderLineForFirstLeapCoroutine = StartCoroutine(RenderLineForFirstLeapJob(_castLength, _castWidth, _line, transform));

        while (_target == null && float.IsPositiveInfinity(_firstLeapPoint.x))
        {
            if (Input.GetMouseButtonDown(0))
            {
                yield return _firstPointForLeapCoroutine = StartCoroutine(FirstVectorForLeap());

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

    private bool IsEnemyBeforePlayer(Vector3 startPos, Vector3 endPos)
    {
        RaycastHit2D hitEnemy = Physics2D.Linecast(startPos, endPos, _targetsLayers);
        return hitEnemy.collider != null;
    }

    private IEnumerator IsEnemyBeforePlayerJob(Vector3 startPos, Vector3 endPos, float rangeLeap, LayerMask enemyLayer)
    {
        while (true)
        {
            Debug.Log("IsEnemyBeforePlayerJob");
            Vector3 direction = (endPos - startPos).normalized;
            float distance = rangeLeap * GlobalVariable.cellSize;

            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, distance, enemyLayer);
            if (hit.collider.gameObject != null && hit.collider.transform != _player.transform)
            {
                _isTargetBeforePlayer = true;
            }
            else
            {
                _isTargetBeforePlayer = false;
            }
            Debug.Log("IsEnemyBeforePlayerJob / IsEnemyBeforePlayer = " + _isTargetBeforePlayer);
            yield return null;
        }  

    }

    private IEnumerator IsEnemyBehindPlayerJob(Vector3 startPos, Vector3 endPos, float rangeLeap, LayerMask enemyLayer)
    {
        while (true) 
        {
            Debug.Log("IsEnemyBehindPlayerJob");
            Vector3 direction = (startPos - endPos).normalized;
            float distance = rangeLeap * GlobalVariable.cellSize;

            RaycastHit2D hit = Physics2D.Raycast(transform.position, -direction, distance, enemyLayer);
            if (hit.collider.gameObject != null && hit.collider.transform != _player.transform)
            {
                _isTargetBehindPlayer = true;
            }
            else
            {
                _isTargetBehindPlayer = false;
            }
            Debug.Log("IsEnemyBehindPlayerJob / _isTargetBehindPlayer = " + _isTargetBehindPlayer);
            yield return null;
        }
    }

    #endregion

    #region RenderingLine

    private void RotateAtMouse(Transform transform)
    {
        Vector3 dir = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
    }

    private IEnumerator RenderLineForFirstLeapJob(float length, float width, AbilityLineRenderer line, Transform lineTransform)
    {
        Debug.Log("LightningMovement / RenderLineFirstLeapJob");
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
        Debug.Log("LightningMovement / RenderLineFirstLeapJob");

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
        Debug.Log("LightningMovement / StopRenderLine");

        if (_renderLineForFirstLeapCoroutine != null)
        {
            Debug.Log($"LightningMovement / StopRenderLine / renderFirstLeapLine = {_renderLineForFirstLeapCoroutine}");
            StopCoroutine(_renderLineForFirstLeapCoroutine);
            _renderLineForFirstLeapCoroutine = null;
        }

        if (_renderLineForSecondLeapCoroutine != null)
        {
            Debug.Log($"LightningMovement / StopRenderLine / renderSecondLeapLine = {_renderLineForSecondLeapCoroutine}");
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

        Debug.Log($"LightningMovement / StopRenderLine / after all destroy LineImages");
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

                _isTarget = IsEnemyBeforePlayer(transform.position, _firstLeapPoint);
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

    #endregion

    #region Leaps
    private void SingleLeap(Vector2 firstLeapPoint, float durationLeap, float rangeLeap)
    {
        if (_isTargetBeforePlayerCoroutine == null)
        {
            _isTargetBeforePlayerCoroutine = StartCoroutine(IsEnemyBeforePlayerJob(_player.transform.position, firstLeapPoint, rangeLeap, _targetsLayers));
        }

        _player.Rb.isKinematic = true;
        _player.Move.enabled = false;

        CmdSingleLeap(firstLeapPoint, durationLeap, rangeLeap);
    }

    private void ExecuteLeaps(Vector2 firstLeapPoint, Vector2 secondLeapPoint, float durationLeap, float rangeLeap)
    {
        Vector3 startPos = _player.transform.position;
        if (_isTargetBeforePlayerCoroutine == null)
        {
            Debug.Log("StartCoroutine BeforePlayer");
            _isTargetBeforePlayerCoroutine = StartCoroutine(IsEnemyBeforePlayerJob(_player.transform.position, firstLeapPoint, rangeLeap, _targetsLayers));
        }
        if (_isTargetBehindPlayerCoroutine == null)
        {
            Debug.Log("StartCoroutine BehindPlayer");
            _isTargetBehindPlayerCoroutine = StartCoroutine(IsEnemyBehindPlayerJob(startPos, firstLeapPoint, rangeLeap, _targetsLayers));
        }

        _player.Rb.isKinematic = true;
        _player.Move.enabled = false;

        CmdExecuteTwoLeaps(firstLeapPoint, secondLeapPoint, durationLeap, rangeLeap, _targetsLayers);
    }
    #endregion

    #region Command

    [Command]
    private void CmdSingleLeap(Vector2 firstLeapPoint, float durationLeap, float rangeLeap)
    {
        _player.CharacterState.AddState(States.Immateriality, 10f, 0, _player.gameObject, Name);

        _player.Move.enabled = false;

        _player.Rb.DOMove(firstLeapPoint, durationLeap * rangeLeap / GlobalVariable.cellSize).SetEase(Ease.Linear);
    }

    [Command]
    private void CmdExecuteTwoLeaps(Vector2 firstLeapPoint, Vector2 secondLeapPoint, float durationLeap, float rangeLeap, LayerMask enemyLayer)
    {
        Debug.Log("CmdExecuteTwoLeaps / isTargetBeforePlayer = " + _isTargetBeforePlayer);
        Debug.Log("CmdExecuteTwoLeaps / isTargetBehindPlayer = " + _isTargetBehindPlayer);
        _player.CharacterState.AddState(States.Immateriality, 10f, 0, _player.gameObject, Name);

        _player.Move.enabled = false;

        DG.Tweening.Sequence leapSequence = DOTween.Sequence();
        leapSequence.Append(_player.Rb.DOMove(firstLeapPoint, durationLeap * rangeLeap / GlobalVariable.cellSize).SetEase(Ease.Linear));
        leapSequence.AppendCallback(() =>
        { 
            if (_isTargetBehindPlayer)
            {
                leapSequence.Append(_player.Rb.DOMove(secondLeapPoint, durationLeap * rangeLeap / GlobalVariable.cellSize).SetEase(Ease.Linear));
            }
            else
            {
                leapSequence.Kill();
            }
        });
    }

    #endregion
}