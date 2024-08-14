using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StandartAutoAttack : AutoAttackSkill
{
    [SerializeField] private float _damage;

    protected override void CastAction()
    {
        _target.Health.CmdTryTakeDamage(Buff.Damage.GetBuffedValue(_damage), DamageType.Physical, AttackRangeType.MeleeAttack);
    }
}
