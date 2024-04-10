using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Telekinesis : Ability
{
    [Header("Ability settings")]
    [SerializeField] private FillAmountOverTime _cooldown;
    [SerializeField] private FillAmountOverTime _castLine;
    [SerializeField] private DrawCircle _drawCircleSelf;
    [SerializeField] private DrawCircle _drawCirclePref;
    [SerializeField] private PlayerMove _playerMove;
    [SerializeField] private float _duration;
    [SerializeField] private float _castDeley;
    [SerializeField] private float _manaCostRate;
    [SerializeField] private float _manaCostPerTick;
    [SerializeField] private float _radius;
    [SerializeField] private float _range;
    
    private DrawCircle _circleTarget;
    private PlayerMove _target;
    private Vector3 _position;
    private Coroutine _useJob;
    private Coroutine _manaCostJob;

    public override void Cancel()
    {
        if(_useJob != null)
            StopCoroutine(_useJob);

        if (_manaCostJob != null)
            StopCoroutine(_manaCostJob);

        ResetValue();

        if(_circleTarget != null)
            Destroy(_circleTarget.gameObject);
    }

    public override void Use()
    {
        _useJob = StartCoroutine(UseCoroutine());
    }

    private void ResetValue()
    {
        if(_target != null)
            _target.CanMove = true;

        _drawCircleSelf.Clear();
        _playerMove.CanMove = true;
        _target = null;
        _position = Vector3.zero;
        _castLine.Stop();
    }

    private bool IsMouseInRadius()
    {
        float distance = Vector3.Distance(
            new Vector3(Camera.main.ScreenToWorldPoint(Input.mousePosition).x, Camera.main.ScreenToWorldPoint(Input.mousePosition).y, transform.position.z),
            transform.position
            );

        return distance <= _radius;
    }

    private bool IsMouseInRange()
    {
        float distance = Vector3.Distance(
            new Vector3(Camera.main.ScreenToWorldPoint(Input.mousePosition).x, Camera.main.ScreenToWorldPoint(Input.mousePosition).y, transform.position.z),
            _target.transform.position
            );

        return distance <= _range;
    }

    private IEnumerator CastDeleyCoroutine()
    {
        _castLine.StartFill(_castDeley);

        float time = 0;
        while (time < _castDeley)
        {
            time += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator ManaCostPerTickCorutine()
    {
        float time = 0;
        while (time < _duration + _manaCostRate)
        {
            Mana.UseMana(_manaCostPerTick);
            time += _manaCostRate;
            yield return new WaitForSeconds(_manaCostRate);
        }
        IsCanCancle = true;
        Cancel();
    }

    private IEnumerator UseCoroutine()
    {
        _drawCircleSelf.Draw(_radius);

        while (_target == null) //выбираем цель
        {
            if (Input.GetMouseButtonDown(0) && IsMouseInRadius())
            {
                RaycastHit2D[] rayHit = Physics2D.RaycastAll(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
                if (rayHit.Length > 0 && rayHit[0].transform.TryGetComponent<PlayerMove>(out PlayerMove enemyMover))
                {
                    _target = enemyMover;
                }
            }
            yield return null;
        }
        _circleTarget = Instantiate(_drawCirclePref, _target.transform);
        _circleTarget.Draw(_range);
        _drawCircleSelf.Clear();

        while (_position == Vector3.zero) //выбираем точку перемещения
        {
            if (Input.GetMouseButtonDown(0))
            {
                _position = new Vector3(Camera.main.ScreenToWorldPoint(Input.mousePosition).x, Camera.main.ScreenToWorldPoint(Input.mousePosition).y, 0);
            }
            yield return null;
        }
        IsCanCancle = false;
        _playerMove.CanMove = false;
        _target.CanMove = false;
        Destroy(_circleTarget.gameObject);
        yield return StartCoroutine(CastDeleyCoroutine());

        float time = 0;
        _manaCostJob = StartCoroutine(ManaCostPerTickCorutine());
        IsCanCancle = true;
        _castLine.StartFill(_duration, 1, 0);

        while (time < _duration)
        {
            time += Time.deltaTime;

            _target.transform.position = Vector2.MoveTowards(_target.transform.position, _position, _range * Time.deltaTime / _duration);
            yield return null;
        }
        ResetValue();
        PayCost();
    }
}
