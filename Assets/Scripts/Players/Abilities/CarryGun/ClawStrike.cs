using Mirror;
using System.Collections;
using UnityEngine;

public class ClawStrike : AutoAttackSkill
{
    [SerializeField] private Character _player;
    [SerializeField] private BasePsionicEnergy _basePsionicEnergy;
    [SerializeField] private AttackingPsionicEnergy _attackingPsionicEnergy;
    [SerializeField] private float _baseDamage;
    [SerializeField] private float animSpeed = 1.2f;

    private float _spentAttackingPsiEnergy;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerAutoAttack => Animator.StringToHash("ClawStrikeTrigger");

    protected override void CastAction()
    {
        if (_target == null) return;
        if (!IsTargetInRange()) return;
        DamageDeal();
    }

    private bool IsTargetInRange()
    {
        return Vector3.Distance(_player.transform.position, _target.transform.position) <= Radius;
    }

    private void DamageDeal()
    {
        float attackingPsiValue = _spentAttackingPsiEnergy;

        var damage = new Damage
        {
            Value = _baseDamage,
            Type = DamageType.Physical,
            PhysicAttackType = AttackRangeType.MeleeAttack,
        };

        CmdApplyDamage(damage, _target.gameObject);

        if (attackingPsiValue > 0)
        {
            var additionalDamage = attackingPsiValue;

            int dispelCount = 0;

            if (attackingPsiValue >= 30) dispelCount = 3;
            else if (attackingPsiValue >= 20) dispelCount = 2;
            else if (attackingPsiValue >= 10) dispelCount = 1;

            if (dispelCount > 0) for (int i = 0; i < dispelCount; i++) CmdDispel(_target, dispelCount);

            var damagePsi = new Damage
            {
                Value = additionalDamage,
                Type = DamageType.Magical,
                PhysicAttackType = AttackRangeType.MeleeAttack,
            };

            CmdApplyDamage(damagePsi, _target.gameObject);
        }

    }

    public void ClawStrikeSpeedAnim()
    {
        _player.Animator.SetFloat("ClawStrikeSpeed", 1f / animSpeed);
        if (_attackingPsionicEnergy.IsAttackingPsiEnergy && _attackingPsionicEnergy.CurrentValue > 0f) TrySpendAttackingPsi();
        else _spentAttackingPsiEnergy = 0;
    }

    public void ClawStrikeCast()
    {
        AnimCastAction();
    }

    public void ClawStrikeEnded()
    {
        AnimCastEnded();
    }

    public void TrySpendAttackingPsi()
    {
        _spentAttackingPsiEnergy = _attackingPsionicEnergy.CurrentValue;
        CmdUseAttackingEnergy(_attackingPsionicEnergy.CurrentValue);
    }

    [Command]
    private void CmdUseAttackingEnergy(float value)
    {
        _attackingPsionicEnergy.CurrentValue -= value;
    }


    [Command]
    private void CmdDispel(Character targetCharacter, float dispelCount)
    {
        targetCharacter.CharacterState.DispelStates(StateType.Magic, targetCharacter.NetworkSettings.TeamIndex, _player.NetworkSettings.TeamIndex, dispelCount > 0);
    }
}
