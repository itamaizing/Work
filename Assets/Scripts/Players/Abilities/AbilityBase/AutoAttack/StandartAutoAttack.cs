using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StandartAutoAttack : AutoAttackSkill
{
    [SerializeField] private float _damage;

    protected override void CastAction()
    {
        Damage damage = new Damage
        {
            Value = Buff.Damage.GetBuffedValue(_damage),
            Type = DamageType,
            PhysicAttackType = AttackRangeType,
        };
        ApplyDamage(damage, _target.gameObject);
    }
}
