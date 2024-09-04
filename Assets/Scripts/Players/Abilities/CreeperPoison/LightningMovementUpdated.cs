using DG.Tweening;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightningMovementUpdated : Skill
{
    [SerializeField] private AcceleratedSlap _acceleratedSlap;

    [Header("Ability properties")]
    [SerializeField] private Character _player;
    [SerializeField] private LightningMovementTalent _lMTalent;

    [SerializeField] private float _rangeLeap;
    [SerializeField] private float _durationLeap;

    private Character _target;

    private Vector3 _firstLeapPoint = Vector3.positiveInfinity;
    private Vector3 _secondLeapPoint;
    private Vector3 _pointForSecondLeap;

    private Coroutine _firstPointForLeapCoroutine;
    private Coroutine _secondPointForLeapCoroutine;
    private Coroutine _midPointForLeapCoroutine;

    private bool _isFirstClickDone = false;
    private bool _isSecondClickDone = false;

    protected override bool IsCanCast => CheckCanCast();

    protected override void ClearData()
    {
        _isFirstClickDone = false;
        _isSecondClickDone = false;
        _firstLeapPoint = Vector3.positiveInfinity;
        _secondLeapPoint = Vector3.zero;

        Debug.Log("LightningMovement / ClearData");

        if (_lMTalent.IsActive)
        {
            // _lMTalent.ResetCharacterResistance();
        }
        
        if(_firstPointForLeapCoroutine != null)
        {
            StopCoroutine(FirstPointForLeap());
            _firstPointForLeapCoroutine = null;
        }

        if (_secondPointForLeapCoroutine != null)
        {
            StopCoroutine(SecondPointForLeap());
            _secondPointForLeapCoroutine = null;
        }

        if (_midPointForLeapCoroutine != null)
        {
            StopCoroutine(MidPoint(_firstLeapPoint));
            _midPointForLeapCoroutine = null;
        }
    }

    protected override IEnumerator PrepareJob()
    {
        Debug.Log("LightningMovement / PrepareJob");
        while (_target == null && float.IsPositiveInfinity(_firstLeapPoint.x))
        {
            Debug.Log("LightningMovement / PrepareJob / after while");
            if (Input.GetMouseButtonDown(0))
            {
                Debug.Log("LightningMovement / PrepareJob / after if");
                _target = GetRaycastTarget();

                Debug.Log("LightningMovement / PrepareJob / if target == null");
                yield return _firstPointForLeapCoroutine = StartCoroutine(FirstPointForLeap());
                
                if (_isFirstClickDone)
                {
                    Debug.Log("LightningMovement / PrepareJob / if firstClickDone == true");
                    yield return _midPointForLeapCoroutine = StartCoroutine(MidPoint(_firstLeapPoint));
                }
            }
            yield return null;
        }

    }

    protected override IEnumerator CastJob()
    {
        if (_isFirstClickDone && _isSecondClickDone)
        {
            if (_target != null)
            {
                if (_lMTalent.IsActive)
                {
                    //_lMTalent.IncreasingResistance();
                    ExecuteLeaps(_firstLeapPoint, _secondLeapPoint, _durationLeap, _rangeLeap);
                }
                else
                {
                    ExecuteLeaps(_firstLeapPoint, _secondLeapPoint, _durationLeap, _rangeLeap);
                }
            }
            else
            {
                SingleLeap(_firstLeapPoint, _durationLeap, _rangeLeap);
                Debug.Log("LightningMovement UseLeapCoroutine / SingleLeap calling + firstLeapPoint == " + _firstLeapPoint);
            }
        }
        yield return null;
    }

    private bool CheckCanCast()
    {
        if (_target == null)
            return Vector3.Distance(_firstLeapPoint, transform.position) <= Radius;

        return Vector3.Distance(_firstLeapPoint, transform.position) <= Radius ||
               Vector3.Distance(_target.transform.position, transform.position) <= Radius;
    }

    private IEnumerator FirstPointForLeap()
    {
        Debug.Log("LightningMovement / FirstPointForLeap");
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

    private IEnumerator SecondPointForLeap()
    {
        Debug.Log("LightningMovement / SecondPointForLeapCoroutine");

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

    private IEnumerator MidPoint(Vector2 firstLeapPoint)
    {
        Debug.Log("LightningMovement / MidPointCoroutine");

        Vector2 originalPoint = firstLeapPoint;
        _pointForSecondLeap = _firstLeapPoint;

        yield return _secondPointForLeapCoroutine = StartCoroutine(SecondPointForLeap());

        _pointForSecondLeap = originalPoint;
    }

    private void SingleLeap(Vector2 firstLeapPoint, float durationLeap, float rangeLeap)
    {
        Debug.Log("LightningMovement SingleLeap work");

        CmdSingleLeap(firstLeapPoint, durationLeap, rangeLeap);

        Debug.Log("LightningMovement SingleLeap call CmdSingleLeap");
    }

    private void ExecuteLeaps(Vector2 firstLeapPoint, Vector2 secondLeapPoint, float durationLeap, float rangeLeap)
    {
        Debug.Log("LightningMovement ExecuteLeaps work");

        CmdExecuteTwoLeaps(firstLeapPoint, secondLeapPoint, durationLeap, rangeLeap);
    }

    //[Command]
    private void CmdSingleLeap(Vector2 firstLeapPoint, float durationLeap, float rangeLeap)
    {
        Debug.Log("LightningMovement CmdSingleLeap work");

        //playerLinks.Move.enabled = false;

        Debug.Log("LightningMovement CmdSingleLeap playerLinks Move false");

        _player.Rb.DOMove(firstLeapPoint, durationLeap * rangeLeap / GlobalVariable.cellSize).SetEase(Ease.Linear);
        //playerLinks.Rb.MovePosition(firstLeapPoint * durationLeap * rangeLeap / GlobalVariable.cellSize);

        Debug.Log("LightningMovement CmdSingleLeap playerLinks MovePos work / firstLeapPoint == " + firstLeapPoint);
    }

    //[Command]
    private void CmdExecuteTwoLeaps(Vector2 firstLeapPoint, Vector2 secondLeapPoint, float durationLeap, float rangeLeap)
    {
        Debug.Log("LightningMovement CmdExecuteTwoLeaps work");

        _player.Move.enabled = false;
        Sequence leapSequence = DOTween.Sequence();
        leapSequence.Append(_player.Rb.DOMove(firstLeapPoint, durationLeap * rangeLeap / GlobalVariable.cellSize).SetEase(Ease.Linear));
        leapSequence.Append(_player.Rb.DOMove(secondLeapPoint, durationLeap * rangeLeap / GlobalVariable.cellSize).SetEase(Ease.Linear));
    }
}
