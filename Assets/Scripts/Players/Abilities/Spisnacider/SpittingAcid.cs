using UnityEngine;

public class SpittingAcid : SkillCreatureCarryGun
{
    [SerializeField] private float minDamage = 1f;
    [SerializeField] private float maxDamage = 4f;
    [SerializeField] private float corrodedDuration = 6f;

    protected override string AnimationTrigger => "AttackSpisnacider";

    protected override void ApplySkillEffect(Character target)
    {
        float dmgValue = UnityEngine.Random.Range(minDamage, maxDamage);

        Damage damage = new Damage
        {
            Value = Buff.Damage.GetBuffedValue(dmgValue),
            Type = DamageType,
            PhysicAttackType = AttackRangeType
        };

        CmdApplyDamage(damage, target.gameObject);
        target.CharacterState.CmdAddState(States.CorrodedArmor, corrodedDuration, 0f, Hero.gameObject, Name);
    }
}

