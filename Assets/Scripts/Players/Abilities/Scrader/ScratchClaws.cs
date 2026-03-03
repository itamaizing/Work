using System;
using UnityEngine;

public class ScratchClaws : SkillCreatureCarryGun
{
    [SerializeField] private float minDamage = 1f;
    [SerializeField] private float maxDamage = 4f;
    [SerializeField] private float bleedingDuration = 3f;
    [SerializeField, Range(0, 1f)] private float bleedingChance = 1f;

    protected override string AnimationTrigger => "AttackScared";

    protected override void ApplySkillEffect(Character target)
    {
        float dmgValue = UnityEngine.Random.Range(minDamage, maxDamage);

        Damage damage = new Damage
        {
            Value = Buff.Damage.GetBuffedValue(dmgValue),
            Type = DamageType,
            PhysicAttackType = AttackRangeType
        };

        if (UnityEngine.Random.value <= bleedingChance)
            target.CharacterState.CmdAddState(States.Bleeding, bleedingDuration, 1f, Hero.gameObject, Name);

        CmdApplyDamage(damage, target.gameObject);
    }
}
