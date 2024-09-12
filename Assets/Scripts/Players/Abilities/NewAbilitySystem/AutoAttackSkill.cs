using System.Collections;
using UnityEngine;

public abstract class AutoAttackSkill : Skill
{
    [Header("AutoAttack settings")]
    [SerializeField] private float _attackZoneSize;
    [SerializeField] protected float _attackSpeed = 1f;

    protected bool _isAutoattackMode = true;
    protected Character _target;
    private Coroutine _autoAttackCoroutine;
    private bool _isAttacking = false;
    private Vector2 _lastTargetPosition;

    public float AttackSpeed { get => Buff.AttackSpeed.GetBuffedValue(_attackSpeed); }
    public Character Target { get => _target; }
    public Vector2 LastTargetPosition { get => _lastTargetPosition; }
    protected override bool IsCanCast { get => NoObstacles(Target.transform.position, _obstacle) && IsTargetInRadius(Radius, Target.transform); }

    protected abstract void CastAction();

    protected override IEnumerator CastJob()
    {
        yield return _autoAttackCoroutine = StartCoroutine(AutoAttackJob());
    }

    protected override void ClearData()
    {
        if (_autoAttackCoroutine != null)
        {
            StopCoroutine(_autoAttackCoroutine);
            _autoAttackCoroutine = null;
        }
        _isAttacking = false;
        _target = null;
    }

    protected override IEnumerator PrepareJob()
    {
        do
        {
            if (Input.GetMouseButton(0))
            {
                _target = GetRaycastTarget();
            }
            yield return null;
        }
        while (Target == null);
    }

    public void Pause()
    {
        if (_autoAttackCoroutine != null)
        {
            StopCoroutine(_autoAttackCoroutine);
            _autoAttackCoroutine = null;
        }
        _isAttacking = false;
    }

    public void Continue()
    {
        if (_autoAttackCoroutine == null && Target != null)
        {
            _autoAttackCoroutine = StartCoroutine(AutoAttackJob());
        }
    }

    protected virtual IEnumerator AutoAttackJob()
    {
        while (Target != null)
        {
            if (IsTargetInRadius(Radius + _attackZoneSize, Target.transform))
            {
                if (IsTargetInRadius(Radius, Target.transform))
                    _isAttacking = true;

                if (_isAttacking && NoObstacles(Target.transform.position, _obstacle))
                {
                    _lastTargetPosition = Target.transform.position;
                    yield return new WaitForSeconds(AttackSpeed);
                    if (IsTargetInRadius(Radius + _attackZoneSize, Target.transform) && NoObstacles(Target.transform.position, _obstacle) && IsCooldowned)
                    {
                        if (TryPayCost())
                        {
                            CastAction();

                            if (_isAutoattackMode == false)
                                ClearData();
                        }
                    }
                }
            }
            else
            {
                _isAttacking = false;
            }
            yield return null;
        }
        _autoAttackCoroutine = null;
    }
}
