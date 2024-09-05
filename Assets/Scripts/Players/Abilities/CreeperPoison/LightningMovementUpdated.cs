using DG.Tweening;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightningMovementUpdated : TargetOrAreaAbility
{
    [Header("Ability properties")]
    [SerializeField] private Character _dad;
    [SerializeField] private LayerMask _obstacleLayerMask;
    [SerializeField] private LayerMask _enemyLayerMask;
    [SerializeField] private VisualRender _abilityRender;

    [SerializeField] private float _rangeLeap;
    [SerializeField] private float _durationLeap;

    private Vector2 _firstLeapPoint;
    private Vector2 _secondLeapPoint;
    private Vector2 _pointForSecondLeap;

    private Coroutine _useCoroutine;
    private Coroutine _secondLeapCoroutine;
    private Coroutine _midPointCoroutine;

    private bool _isClick = false;
    private bool _isEnemy = false;

    protected override IEnumerator UseCoroutine()
    {
        Debug.Log("LightningMovement UseCoroutine work");
        yield return _chooseTargetJob = StartCoroutine(ChooseTargetCoroutine(Radius));
        CastAction();
    }

    protected override void CastAction()
    {
        Debug.Log("LightningMovement CastAction work");
        _firstLeapPoint = Point;
        _useCoroutine = StartCoroutine(UseLeapCoroutine());
    }

    protected override void Cancel()
    {
        _isClick = false;

        Debug.Log("LightningMovement Cancel work");

        if (_useCoroutine != null)
            StopCoroutine(UseLeapCoroutine());

        if (_secondLeapCoroutine != null)
            StopCoroutine(SecondPointForLeapCoroutine());

        if (_midPointCoroutine != null)
            StopCoroutine(MidPointCoroutine(_firstLeapPoint));
    }

    private IEnumerator UseLeapCoroutine()
    {
        Debug.Log("LightningMovement UseLeapCoroutine work");

        PayCost();
        if (Target != null)
        {
            yield return _midPointCoroutine = StartCoroutine(MidPointCoroutine(_firstLeapPoint));
            ExecuteLeaps(_dad, _firstLeapPoint, _secondLeapPoint, _durationLeap, _rangeLeap); 
        }
        else
        {
            SingleLeap(_dad, _firstLeapPoint, _durationLeap, _rangeLeap);
            Debug.Log("LightningMovement UseLeapCoroutine / SingleLeap calling + firstLeapPoint == " + _firstLeapPoint);
        }
    }

    private IEnumerator SecondPointForLeapCoroutine()
    {
        Debug.Log("LightningMovement SecondPointForLeapCoroutine work");

        while (!_isClick)
        {
            if (Input.GetMouseButtonDown(0))
            {
                _isClick = true;
                _secondLeapPoint = GetMousePoint();
            }
            yield return null;
        }
    }

    private IEnumerator MidPointCoroutine(Vector2 firstLeapPoint)
    {
        Debug.Log("LightningMovement MidPointCoroutine work");

        Vector2 originalPoint = _firstLeapPoint;
        _pointForSecondLeap = _firstLeapPoint;

        _abilityRender.Drawn(this);

        yield return _secondLeapCoroutine = StartCoroutine(SecondPointForLeapCoroutine());

        _pointForSecondLeap = originalPoint;
    }

    private void SingleLeap(Character playerLinks, Vector2 firstLeapPoint, float durationLeap, float rangeLeap)
    {
        Debug.Log("LightningMovement SingleLeap work");

        CmdSingleLeap(playerLinks, firstLeapPoint, durationLeap, rangeLeap);

        Debug.Log("LightningMovement SingleLeap call CmdSingleLeap");
    }

    private void ExecuteLeaps(Character playerLinks, Vector2 firstLeapPoint, Vector2 secondLeapPoint, float durationLeap, float rangeLeap)
    {
        Debug.Log("LightningMovement ExecuteLeaps work");

        CmdExecuteTwoLeaps(playerLinks, firstLeapPoint, secondLeapPoint, durationLeap, rangeLeap);
    }

    //[Command]
    private void CmdSingleLeap(Character playerLinks,Vector2 firstLeapPoint, float durationLeap, float rangeLeap)
    {
        Debug.Log("LightningMovement CmdSingleLeap work");

        //playerLinks.Move.enabled = false;

        Debug.Log("LightningMovement CmdSingleLeap playerLinks Move false");
		Debug.LogError("fix");
		//playerLinks.Rigidbody2D.DOMove(firstLeapPoint, durationLeap * rangeLeap / GlobalVariable.cellSize).SetEase(Ease.Linear).OnComplete(Cancel);
        //playerLinks.Rb.MovePosition(firstLeapPoint * durationLeap * rangeLeap / GlobalVariable.cellSize);

        Debug.Log("LightningMovement CmdSingleLeap playerLinks MovePos work / firstLeapPoint == " + firstLeapPoint);

        Cancel();
    }

    //[Command]
    private void CmdExecuteTwoLeaps(Character playerLinks, Vector2 firstLeapPoint, Vector2 secondLeapPoint, float durationLeap, float rangeLeap)
    {
        //Debug.Log("LightningMovement CmdExecuteTwoLeaps work");

        playerLinks.Move.enabled = false;
        Sequence leapSequence = DOTween.Sequence();
		Debug.LogError("fix");
		//leapSequence.Append(playerLinks.Rigidbody2D.DOMove(firstLeapPoint, durationLeap * rangeLeap / GlobalVariable.cellSize).SetEase(Ease.Linear));
        //leapSequence.Append(playerLinks.Rigidbody2D.DOMove(secondLeapPoint, durationLeap * rangeLeap / GlobalVariable.cellSize).SetEase(Ease.Linear));
        leapSequence.OnComplete(Cancel);
    }
}
