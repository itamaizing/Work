using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StandartAutoAttack : AutoAttackAbility
{
    [SerializeField] private float _damage;

    protected override void Cancel() { }

    protected override void CastAction()
    {
        Target.Health.TakeDamage(_damage, DamageType.Physical);
    }
}
