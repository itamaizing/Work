using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class NewFourMeleeAttack : Ability
{
    [Header("Ability settings")]
    [SerializeField] private float _range;
    [SerializeField] private Tentacles _tentaclesPrefab;
    [SerializeField] private DamageType _damageType;
    private MoveComponent _target;
    private Vector3 _position;
    private Coroutine _useJob;

    private List<Transform> _enemies = new List<Transform>(); 
    private Tentacles _tentacles;

    protected override void Cancel()
    {
        if (_useJob != null)
            StopCoroutine(_useJob);

        ResetValue();
    }

    protected override void Cast()
    {
        _useJob = StartCoroutine(UseCoroutine());
    }

    private void ResetValue()
    {
        if (_target != null)
            _target.CanMove = true;

        PlayerMove.CanMove = true;
        _target = null;
        _position = Vector3.zero;
        _enemies.Clear();

        if(_tentacles != null) Destroy(_tentacles.gameObject);
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
        yield return GetCastDeleyCoroutine();

        float time = 0;
        IsCanCancle = true;
        //PayCost();
        _tentacles = Instantiate(_tentaclesPrefab, _target.transform);
        var targetpos = _target.transform.position;

        _enemies.Add(_target.transform);
        
        while (time < StreamingDuration) // действие 
        {

            time += Time.deltaTime;

            _target.transform.position = Vector2.MoveTowards(_target.transform.position, _position, _range * Time.deltaTime /*/ StreamingDuration*/);
            yield return null;
        }
        _target.GetComponent<HealthComponent>().TryTakeDamage(10, _damageType, AttackRangeType.MeleeAttack);
        ResetValue();
    }
}
