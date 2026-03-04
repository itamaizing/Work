using UnityEngine;

public class PowerStrike : SkillCreatureCarryGun
{
    [SerializeField] private float minDamage = 12f;
    [SerializeField] private float maxDamage = 18f;
    [SerializeField] private float aoeRadius = 1.5f;

    protected override string AnimationTrigger => "AttackGetomir";

    protected override void ApplySkillEffect(Character mainTarget)
    {
        float baseDamage = Buff.Damage.GetBuffedValue(
            UnityEngine.Random.Range(minDamage, maxDamage));

        Collider[] hits = Physics.OverlapSphere(mainTarget.transform.position, aoeRadius, Targeting.Layer);

        foreach (var hit in hits)
        {
            Character character = hit.GetComponent<Character>();
            if (character == null || character == Hero || character.IsDead)
                continue;

            float final = character == mainTarget ? baseDamage : baseDamage * 0.5f;

            Damage damage = new Damage
            {
                Value = final,
                Type = Info.DamageType,
                PhysicAttackType = Info.AttackRangeType
            };

            CmdApplyDamage(damage, character.gameObject);
        }
    }
}
