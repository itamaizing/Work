using UnityEngine;

public class DeadStrike : SkillCreatureIceDeath
{
    [SerializeField] private float minDamage = 15f;
    [SerializeField] private float maxDamage = 22f;
    [SerializeField] private float aoeRadius = 1.5f;

    private const float PlagueDuration = 12f;

    protected override string AnimationTrigger => string.Empty;

    protected override void ApplySkillEffect(Character mainTarget)
    {
        if (mainTarget == null || mainTarget.IsDead) return;

        float baseDamage = Buff.Damage.GetBuffedValue(Random.Range(minDamage, maxDamage));

        Collider[] hits = Physics.OverlapSphere(mainTarget.transform.position, aoeRadius, Targeting.Layer);

        foreach (var hit in hits)
        {
            Character character = hit.GetComponent<Character>();
            if (character == null || character == Hero || character.IsDead) continue;

            float finalDamage = character == mainTarget ? baseDamage : baseDamage * 0.5f;

            Damage damage = new Damage
            {
                Value = finalDamage,
                Type = Info.DamageType,
                PhysicAttackType = Info.AttackRangeType
            };

            CmdApplyDamage(damage, character.gameObject);

            character.CharacterState.AddState(States.Plague, PlagueDuration, 0, Hero.gameObject, name);
        }
    }
}