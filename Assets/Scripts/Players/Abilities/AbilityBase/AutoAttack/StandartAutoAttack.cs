using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StandartAutoAttack : AutoAttackSkill
{
    [SerializeField] private float _damage;

    protected override void CastAction()
    {
        CmdApplyDamage(Buff.Damage.GetBuffedValue(_damage), _target.gameObject);
    }
}
