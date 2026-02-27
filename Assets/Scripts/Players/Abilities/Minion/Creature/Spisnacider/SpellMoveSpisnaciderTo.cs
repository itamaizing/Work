using UnityEngine;

public class SpellMoveSpisnaciderTo : SpellMoveCreatureTo
{
    [SerializeField] private float minDamage = 8f;
    [SerializeField] private float maxDamage = 10f;

    protected override string AutoAttackTrigger => "AutoAttackSpisnacider";

    protected override void DealDamage(Character target)
    {
        float roll = UnityEngine.Random.Range(minDamage, maxDamage);

        Damage dmg = new Damage
        {
            Value = Buff.Damage.GetBuffedValue(roll),
            Type = DamageType,
            PhysicAttackType = AttackRangeType
        };

        CmdApplyDamage(dmg, target.gameObject);
    }
}