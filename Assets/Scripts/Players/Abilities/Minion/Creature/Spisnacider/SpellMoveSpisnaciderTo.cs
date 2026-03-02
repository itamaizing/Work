using UnityEngine;

public class SpellMoveSpisnaciderTo : SpellMoveCreatureTo
{
    [SerializeField] private float minDamage = 8f;
    [SerializeField] private float maxDamage = 10f;

    protected override string AutoAttackTrigger => "AutoAttackSpisnacider";

    protected override void DealDamage(Character target)
    {
        float randomDamage = Random.Range(minDamage, maxDamage);

        Damage damage = new Damage
        {
            Value = Buff.Damage.GetBuffedValue(randomDamage),
            Type = DamageType,
            PhysicAttackType = AttackRangeType
        };

        CmdApplyDamage(damage, target.gameObject);
    }
}