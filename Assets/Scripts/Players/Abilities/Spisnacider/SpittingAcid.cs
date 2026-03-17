using UnityEngine;

public class SpittingAcid : SkillCreatureCarryGun
{
    [SerializeField] private float minDamage = 1f;
    [SerializeField] private float maxDamage = 4f;
    [SerializeField] private float corrodedDuration = 6f;

    protected override string AnimationTrigger => "SpittingAcid";

    protected override void ApplySkillEffect(Character target)
    {
        float dmgValue = Random.Range(minDamage, maxDamage);

        Damage damage = new Damage
        {
            Value = Buff.Damage.GetBuffedValue(dmgValue),
            Type = Info.DamageType,
            PhysicAttackType = Info.AttackRangeType
        };

        CmdApplyDamage(damage, target.gameObject);
        target.CharacterState.CmdAddState(States.CorrodedArmor, corrodedDuration, 0f, Hero.gameObject, Name);
    }
}

