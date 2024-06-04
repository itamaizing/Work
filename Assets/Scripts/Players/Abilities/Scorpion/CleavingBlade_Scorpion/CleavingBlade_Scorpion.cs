using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CleavingBlade_Scorpion : Ability
{
    [Header("Ability settings")]
    [SerializeField] private DrawCircle _drawCircleSelf;
    [SerializeField] private float _damageValue;

    private DrawCircle _circleTarget;
    private HealthComponent _target;
    private Coroutine _useJob;
    private int _counter = 1; // временно вместо бафа
    protected override void Cancel()
    {
        if (_useJob != null)
            StopCoroutine(_useJob);

        ResetValue();

        if (_circleTarget != null)
            Destroy(_circleTarget.gameObject);
    }

    protected override void Cast()
    {
        _useJob = StartCoroutine(UseCoroutine());
    }

    private void ResetValue()
    {
        _drawCircleSelf.Clear();
        _target = null;
    }

    private bool IsMouseInRadius()
    {
        float distance = Vector3.Distance(
            new Vector3(Camera.main.ScreenToWorldPoint(Input.mousePosition).x, Camera.main.ScreenToWorldPoint(Input.mousePosition).y, transform.position.z),
            transform.position
            );

        return distance <= Radius;
    }
    private void AttackPassed()
    {
        if(_counter == 3)
        {
            _counter = 1;
        }
        else
        {
            _counter++;
        }
    }
    private IEnumerator UseCoroutine()
    {
        _drawCircleSelf.Draw(Radius);

        while (_target == null) //выбираем цель
        {
            if (Input.GetMouseButtonDown(0) && IsMouseInRadius())
            {
                RaycastHit2D[] rayHit = Physics2D.RaycastAll(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
                if (rayHit.Length > 0 && rayHit[0].transform.TryGetComponent<HealthComponent>(out HealthComponent enemyHealth))
                {
                    _target = enemyHealth;
                    Debug.LogWarning(_target.name);
                }
            }
            yield return null;
        }
        _drawCircleSelf.Clear();

        IsCanCancle = false;
        float initialCastDelay = CastDeley;
        if (_counter == 2) CastDeley *= 0.8f;

        yield return GetCastDeleyCoroutine();

        CastDeley = initialCastDelay;

        IsCanCancle = true;
        PayCost();

        if (_target != null && Vector2.Distance(transform.position, _target.transform.position) <= 2f + 0.19f)
        {
            float damage = _damageValue;
            if (_counter == 3)
            {
                damage = _damageValue * 2;
            }
            if (_target.TryTakeDamage(damage, DamageType.Physical, AttackRangeType.MeleeAttack))
            {
                AttackPassed();
            }
            else _counter = 1;
        }
        Debug.LogWarning(_counter);

        ResetValue();
    }
}
