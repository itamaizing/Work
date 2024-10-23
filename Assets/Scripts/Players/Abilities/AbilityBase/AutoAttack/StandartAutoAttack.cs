using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StandartAutoAttack : AutoAttackSkill
{
    [SerializeField] private float _damage;

    protected override int AnimTriggerCastDelay => throw new System.NotImplementedException();

    protected override int AnimTriggerCast => throw new System.NotImplementedException();

    protected override void CastAction()
    {
        Damage damage = new Damage
        {
            Value = Buff.Damage.GetBuffedValue(_damage),
            Type = DamageType,
            Range = AttackRangeType,
        };
        CmdApplyDamage(damage, _target.gameObject);
    }
}
