using UnityEngine;

public class StandartAutoAttack : AutoAttackSkill
{
    [SerializeField] private float _damage;

    protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerAutoAttack => Animator.StringToHash("AtackStandartAutoAttack");

    public void AnimStandartAutoAttackCast()
    {
        AnimCastAction();
    }

    public void AnimStandartAutoAttackEnded()
    {
        AnimCastEnded();
    }

protected override void CastAction()
    {
        if (_target == null)
            return;

        Damage damage = new Damage
        {
            Value = Buff.Damage.GetBuffedValue(_damage),
            Type = DamageType,
            School = School,
            Form = AbilityForm,
            PhysicAttackType = AttackRangeType,

        };
        CmdApplyDamage(damage, _target.gameObject);
    }
}
