using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClawStrike : AutoAttackSkill
{
    [SerializeField] private Character _player;
    [SerializeField] private BasePsionicEnergy _basePsionicEnergy;
    [SerializeField] private AttackingPsionicEnergy _attackingPsionicEnergy;
    [SerializeField] private float _baseDamage;

    private Damage _damage = new Damage();

    private int _maxCountDispelState;

    private float _additionalDamage;

    protected override void CastAction()
    {
        DamageDeal();
    }

    private void DamageDeal()
    {
        if (_attackingPsionicEnergy.IsAttackingPsiEnergy)
        {
            _additionalDamage = _attackingPsionicEnergy.CurrentAttackingPsiEnergy;

            if (_additionalDamage > 10 && _additionalDamage < 20)
            {
                _target.CharacterState.DispelOneState(StateType.Magic);
            }
            else if (_additionalDamage > 20 && _additionalDamage < 30)
            {
                _maxCountDispelState = 2;
                for (int i = 0; i < _maxCountDispelState; i++)
                {
                    _target.CharacterState.DispelOneState(StateType.Magic);
                }
            }
            else if (_additionalDamage == 30)
            {
                _maxCountDispelState = 3;
                for (int i = 0; i < _maxCountDispelState; i++)
                {
                    _target.CharacterState.DispelOneState(StateType.Magic);
                }
            }

            _damage = new Damage
            {
                Value = _baseDamage + _additionalDamage,
                Type = DamageType.Physical,
                Range = AttackRangeType.MeleeAttack
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
                Range = AttackRangeType.MeleeAttack
            };
            CmdApplyDamage(_damage, _target.gameObject);

            CmdIncreaseEnergy(_damage.Value);

            _damage.Value = 0f;
        }
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
}
