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

    private Damage _damage = new Damage();

    private int _maxCountDispelState;

    private float _additionalDamage;
    protected override int AnimTriggerCast => 0;
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerAutoAttack => Animator.StringToHash("ClawStrikeTrigger");

    protected override void CastAction()
    {
        if (_target == null) return;
        DamageDeal();
    }

    private bool IsTargetInRange()
    {
        float maxDistance = Radius * 1.1f;
        return Vector3.Distance(_player.transform.position, _target.transform.position) <= maxDistance;
    }

    private void DamageDeal()
    {
        if (_attackingPsionicEnergy.IsAttackingPsiEnergy)
        {
            _additionalDamage = _attackingPsionicEnergy.CurrentAttackingPsiEnergy;

            if (_additionalDamage > 10 && _additionalDamage < 20)
            {
                _target.CharacterState.DispelStates(StateType.Magic, _target.NetworkSettings.TeamIndex, _player.NetworkSettings.TeamIndex, true);
            }
            else if (_additionalDamage > 20 && _additionalDamage < 30)
            {
                _maxCountDispelState = 2;
                for (int i = 0; i < _maxCountDispelState; i++)
                {
                    _target.CharacterState.DispelStates(StateType.Magic, _target.NetworkSettings.TeamIndex, _player.NetworkSettings.TeamIndex, true);
                }
            }
            else if (_additionalDamage == 30)
            {
                _maxCountDispelState = 3;
                for (int i = 0; i < _maxCountDispelState; i++)
                {
                    _target.CharacterState.DispelStates(StateType.Magic, _target.NetworkSettings.TeamIndex, _player.NetworkSettings.TeamIndex, true);
                }
            }

            _damage = new Damage
            {
                Value = _baseDamage + _additionalDamage,
                Type = DamageType.Physical,
                PhysicAttackType = AttackRangeType.MeleeAttack,
            };

            CmdUseAttackingEnergy(_additionalDamage);

            CmdApplyDamage(_damage, _target.gameObject);

            _additionalDamage = 0;
            _maxCountDispelState = 0;
            _damage.Value = 0f;
        }
        else
        {
            _damage = new Damage
            {
                Value = _baseDamage,
                Type = DamageType.Physical,
                PhysicAttackType = AttackRangeType.MeleeAttack,
            };
            CmdApplyDamage(_damage, _target.gameObject);

            CmdIncreaseEnergy(_damage.Value);

            _damage.Value = 0f;
        }
    }

    public void ClawStrikeSpeedAnim()
    {
        _player.Animator.SetFloat("ClawStrikeSpeed", 1f / animSpeed);
    }

    public void ClawStrikeCast()
    {
        AnimCastAction();
    }

    public void ClawStrikeEnded()
    {
        AnimCastEnded();
    }

    [Command]
    private void CmdIncreaseEnergy(float value)
    {
        _basePsionicEnergy.IncreasePsiEnergy(value);
    }

    [Command]
    private void CmdUseAttackingEnergy(float value)
    {
        _attackingPsionicEnergy.CurrentAttackingPsiEnergy -= value;
    }

    protected override void ClearData()
    {
        base.ClearData();
    }
}
