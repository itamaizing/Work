using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StandartRangeAutoAttack : AutoAttackAbility
{
    [SerializeField] private Projectile _projectilePref;

    private Projectile _projectile;
    protected override void Cancel()
    {
        _projectile = null;
    }

    protected override void CastAction()
    {
        _projectile = Instantiate(_projectile);
    }
}
