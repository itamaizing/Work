using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Kick_Scorpion : Ability
{
    [Header("Ability settings")]
    [SerializeField] private DrawCircle _drawCircleSelf;
    [SerializeField] private Sub_LavaPool_Scorpion _pool;
    [SerializeField] private Counter_ScorchedSoul_Baff _comboCounterPrefab;
    [SerializeField] private float _damageValue = 15f; // потом сделать разброс 10-15
    private Counter_ScorchedSoul_Baff _newprefab;

    private DrawCircle _circleTarget;
    private HealthComponent _target;
    private Coroutine _useJob;

    protected override void Cancel()
    {
        if (_useJob != null)
            StopCoroutine(_useJob);

        ResetValue();

        if (_circleTarget != null)
            Destroy(_circleTarget.gameObject);
    }
    private void AttackPassed()
    {
        if (_newprefab == null) // заглушка, жду новую базу под бафы
        {
            _newprefab = Instantiate(_comboCounterPrefab, PlayerMove.transform);
        }
        else
        {
            if (_newprefab.CurrentStacks == 2)
            {
                Instantiate(_pool, _target.transform.position, Quaternion.identity).Init();
            }
            _newprefab.AddStack();
        }
        Debug.LogWarning(_newprefab.CurrentStacks);
    }
    protected override void Cast()
    {
        _useJob = StartCoroutine(UseCoroutine());
    }

    private void ResetValue()
    {
        _drawCircleSelf.Clear();
        _target = null;
        PlayerMove.CanMove = true;
    }

    private bool IsMouseInRadius()
    {
        float distance = Vector3.Distance(
            new Vector3(Camera.main.ScreenToWorldPoint(Input.mousePosition).x, Camera.main.ScreenToWorldPoint(Input.mousePosition).y, transform.position.z),
            transform.position
            );

        return distance <= Radius;
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
                }
            }
            yield return null;
        }
        _drawCircleSelf.Clear();

        PlayerMove.CanMove = false;
        IsCanCancle = false;

        yield return GetCastDeleyCoroutine();

        IsCanCancle = true;
        PayCost();

        if (Vector2.Distance(transform.position, _target.transform.position) <= 2f + 0.19f)
        {
            if (_target.TryTakeDamage(_damageValue, DamageType.Physical, AttackRangeType.MeleeAttack))
            {
                AttackPassed();
            }
        }
        ResetValue();
    }
}
