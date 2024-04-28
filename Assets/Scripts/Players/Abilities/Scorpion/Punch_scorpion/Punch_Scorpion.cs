using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Punch_Scorpion : Ability
{
    [Header("Ability settings")]
    [SerializeField] private DrawCircle _drawCircleSelf;
    [SerializeField] private float _range;

    private DrawCircle _circleTarget;
    private PlayerMove _target;
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

    private IEnumerator UseCoroutine()
    {
        _drawCircleSelf.Draw(Radius);

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
        _drawCircleSelf.Clear();

        IsCanCancle = false;

        yield return GetCastDeleyCoroutine();

        IsCanCancle = true;
        PayCost();

        _target.GetComponent<HealthPlayer>().TakeDamage(9, DamageType.Physical, AttackRangeType.MeleeAttack);

        ResetValue();
    }
}
