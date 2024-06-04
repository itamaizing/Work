using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Telekinesis : Ability
{
    [Header("Ability settings")]
    [SerializeField] private DrawCircle _drawCircleSelf;
    [SerializeField] private DrawCircle _drawCirclePref;
    [SerializeField] private float _range;
    
    private DrawCircle _circleTarget;
    private MoveComponent _target;
    private Vector3 _position;
    private Coroutine _useJob;

    protected override void Cancel()
    {
        if(_useJob != null)
            StopCoroutine(_useJob);

        ResetValue();

        if(_circleTarget != null)
            Destroy(_circleTarget.gameObject);
    }

    protected override void Cast()
    {
        _useJob = StartCoroutine(UseCoroutine());
    }

    private void ResetValue()
    {
        if(_target != null)
            _target.CanMove = true;

        _drawCircleSelf.Clear();
        PlayerMove.CanMove = true;
        _target = null;
        _position = Vector3.zero;
    }

    private bool IsMouseInRadius()
    {
        float distance = Vector3.Distance(
            new Vector3(Camera.main.ScreenToWorldPoint(Input.mousePosition).x, Camera.main.ScreenToWorldPoint(Input.mousePosition).y, transform.position.z),
            transform.position
            );

        return distance <= Radius;
    }

    private bool IsMouseInRange()
    {
        float distance = Vector3.Distance(
            new Vector3(Camera.main.ScreenToWorldPoint(Input.mousePosition).x, Camera.main.ScreenToWorldPoint(Input.mousePosition).y, transform.position.z),
            _target.transform.position
            );

        return distance <= _range;
    }

    private IEnumerator UseCoroutine()
    {
        _drawCircleSelf.Draw(Radius);

        while (_target == null) //выбираем цель
        {
            if (Input.GetMouseButtonDown(0) && IsMouseInRadius())
            {
                RaycastHit2D[] rayHit = Physics2D.RaycastAll(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
                if (rayHit.Length > 0 && rayHit[0].transform.TryGetComponent<MoveComponent>(out MoveComponent enemyMover))
                {
                    _target = enemyMover;
                }
            }
            yield return null;
        }
        AreaOff();
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
        PlayerMove.CanMove = false;
        _target.CanMove = false;
        Destroy(_circleTarget.gameObject);
        yield return GetCastDeleyCoroutine();

        float time = 0;
        IsCanCancle = true;
        PayCost();

        while (time < StreamingDuration) // действие 
        {
            time += Time.deltaTime;

            _target.transform.position = Vector2.MoveTowards(_target.transform.position, _position, _range * Time.deltaTime / StreamingDuration);
            yield return null;
        }
        ResetValue();
    }
}
