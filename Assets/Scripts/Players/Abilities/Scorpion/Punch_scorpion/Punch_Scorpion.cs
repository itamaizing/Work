using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Punch_Scorpion : Ability
{
    [Header("Ability settings")]
    [SerializeField] private DrawCircle _drawCircleSelf;
    [SerializeField] private float _range;
    [SerializeField] private Counter_ScorchedSoul_Baff _comboCounterPrefab;
    private Counter_ScorchedSoul_Baff _comboCounterBaff;

    private DrawCircle _circleTarget;
    private HealthPlayer _target;
    private Coroutine _useJob;

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
        _target.onDamagePassed -= AttackPassed;
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

    private void AttackPassed(bool isAttackPassed)
    {
        if (isAttackPassed) 
        {
            Debug.LogWarning("Попал");

            if (_comboCounterBaff == null) // заглушка, жду новую базу под бафы
            {
                _comboCounterBaff = Instantiate(_comboCounterPrefab, PlayerMove.transform);
                Debug.LogWarning(_comboCounterBaff.CurrentStacks);
            }
            else
            {
                _comboCounterBaff.AddStack();
                Debug.LogWarning(_comboCounterBaff.CurrentStacks);
            }
        }
        else
        {
            Debug.LogWarning("Не попал");
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
                if (rayHit.Length > 0 && rayHit[0].transform.TryGetComponent<HealthPlayer>(out HealthPlayer enemyHealth))
                {
                    _target = enemyHealth;
                    _target.onDamagePassed += AttackPassed;
                }
            }
            yield return null;
        }
        _drawCircleSelf.Clear();

        IsCanCancle = false;

        yield return GetCastDeleyCoroutine();

        IsCanCancle = true;
        PayCost();

        if (Vector2.Distance(transform.position, _target.transform.position) >= 1.9f + 0.1f + 0.19f)
        {
            _target.SendAttackPassed(false);
        }
        else
        {
            _target.TakeDamage(9, DamageType.Physical, AttackRangeType.MeleeAttack);
        }

        ResetValue();
    }
}
