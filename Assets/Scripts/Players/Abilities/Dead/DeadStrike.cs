using UnityEngine;

public class DeadStrike : SkillCreatureIceDeath
{
    [SerializeField] private float minDamage = 1f;
    [SerializeField] private float maxDamage = 3f;

    private const float PlagueDuration = 12f;
    private const float PlagueChance = 0.10f;

    protected override string AnimationTrigger => string.Empty;

    protected override void ApplySkillEffect(Character mainTarget)
    {
        if (mainTarget == null || mainTarget.IsDead) return;
        float damageValue = Buff.Damage.GetBuffedValue(Random.Range(minDamage, maxDamage));

        Damage damage = new Damage
        {
            Value = damageValue,
            Type = Info.DamageType,
            PhysicAttackType = Info.AttackRangeType
        };

        CmdApplyDamage(damage, mainTarget.gameObject);

        if (Random.value <= PlagueChance) mainTarget.CharacterState.AddState(States.Plague, PlagueDuration, 0, Hero.gameObject, name);
    }
}