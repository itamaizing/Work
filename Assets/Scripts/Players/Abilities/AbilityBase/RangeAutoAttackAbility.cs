using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class RangeAutoAttackAbility : AutoAttackAbility
{
    [SerializeField] protected Projectile _projectilePref;

    protected Projectile _projectile;

    protected override void Cleaning()
    {
        base.Cleaning();

        _projectile = null;
    }
}
