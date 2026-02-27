using UnityEngine;
using DG.Tweening;

public class SpellMoveScraderTo : SpellMoveCreatureTo
{
    [SerializeField] private float damage = 5f;

    protected override string AutoAttackTrigger => "AutoAttackScrader";

    protected override void DealDamage(Character target)
    {
        Damage dmg = new Damage
        {
            Value = Buff.Damage.GetBuffedValue(damage),
            Type = Info.DamageType,
            PhysicAttackType = Info.AttackRangeType
        };

        CmdApplyDamage(dmg, target.gameObject);
    }
}