using Mirror;
using System.Linq.Expressions;
using UnityEngine;

public class Punch_Scorpion : AutoAttackSkill
{
    [Header("Ability settings")]
    [SerializeField] private Character _playerLinks;
    [SerializeField] private PassiveCombo_Scorpion _comboCounter;
    //[SerializeField] private float _damageValue = 9f;

    private Character _lastTarget = null;

    protected override int AnimTriggerCastDelay => throw new System.NotImplementedException();

    protected override int AnimTriggerCast => throw new System.NotImplementedException();

    protected override void CastAction()
    {
        if (_lastTarget != null && _lastTarget != _target) //сброс
        {
            _comboCounter.ResetCounter();
        }
        Debug.Log(transform.position);
        Debug.Log(_target.transform.position);

        //Vector3 closestEnemyPoint = _target.gameObject.GetComponent<CircleCollider2D>().ClosestPoint(transform.position);
        //Vector3 closestMyPoint = transform.parent.parent.GetComponent<CircleCollider2D>().ClosestPoint(_target.transform.position);


        if (Vector2.Distance(LastTargetPosition, _target.transform.position) <= 2f)
        {
            Debug.Log("Достаточное расстояние");

            Damage damage = new Damage   
            {
                Value = Buff.Damage.GetBuffedValue(_damageValue),
                Type = DamageType,
                Range = AttackRangeType,
            };

            CmdAttack(damage, _target.gameObject);
        }
        else Debug.LogWarning("слишком далеко");
        _lastTarget = _target;

    }
    private void AttackPassed(Transform target)
    {
        Debug.LogWarning("Punch_Scorppion .AttackPassed - Попал");

        _comboCounter.AddAbility(target, ScorpionAbility.Punch);
    }
    private void AttackMissed()
    {
        Debug.LogWarning("Punch_Scorppion .AttackMissed -Промах");

        _comboCounter.ResetCounter();
    }

    [Command]
    private void CmdAttack(Damage damage, GameObject hp)
    {
        if (_tempTargetForDamage != hp.transform)
        {
            _tempTargetForDamage = hp.transform;
            _tempHPForDamage = hp.GetComponent<Health>();
        }

        bool result = _tempHPForDamage.TryTakeDamage(ref damage, this);
        RpcSelfNotifyHitResult(result, _tempTargetForDamage);

    }

    [TargetRpc]
    private void RpcSelfNotifyHitResult(bool state,Transform target)
    {
        if (state)
        {
            AttackPassed(target);
        }
        else
        {
            AttackMissed();
        }
    }
}
