using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreeperStrike : AutoAttackAbility
{
    [SerializeField] LightningStrikes lightningStrikes;

    [SerializeField] protected float _damageDeal = 0.0f;
    [SerializeField] protected Character dad;
    
    [HideInInspector] public float _currentRadius;
    [HideInInspector] public float _currentAttackSpeed;
    [HideInInspector] public Character _currentTarget;
    [HideInInspector] public float _originalAttackSpeed;
    private bool _enabled = false;

    [HideInInspector] public float AttackSpeed => _attackSpeed;
    protected float ThisRadius => _currentRadius;
    private void Update()
    {
        if (!_enabled) return;

        Continue();
        if (lightningStrikes._isUsing)
        {
            lightningStrikes.DecreaseAttackSpeed(_currentAttackSpeed);
        }
    }
    protected override void Cancel()
    {
        _enabled = false;
    }

    protected override void CastAction()
    {
        Debug.Log("Its CreeperAttack CastAction Method. He work");
        _enabled = true;
        Strike(Target);
        Debug.Log(_attackSpeed + " attackSpeed Strikes");
    }

    public void Strike(Character enemy)
    {
        _currentTarget = enemy;
        _currentRadius = _attackZoneSize;
        Debug.Log("Strike at: " +  enemy);
        float currentDamage = _damageDeal + Random.Range(7, 11);
        MakeDamage(enemy.Health, currentDamage, DamageType.Physical, AttackRangeType.MeleeAttack);
        Debug.Log("Deal Damage: " +  currentDamage + " on enemy " + enemy);
    }

    public void ModifyAttackSpeed(float _attackSpeedStrikes)
    {
        _currentAttackSpeed = _attackSpeedStrikes;
    }
    public void ResetAttackSpeed()
    {
        _currentAttackSpeed = _originalAttackSpeed;
    }

    
    private void MakeDamage(HealthComponent _target, float _damage, DamageType _damageType, AttackRangeType _attackRangeType)
    {
        CmdApplyDamage(_target.gameObject, _damage, _damageType, _attackRangeType);
    }

    [Command]
    private void CmdApplyDamage(GameObject target, float damage, DamageType damageType, AttackRangeType attackRangeType)
    {
        target.GetComponent<HealthComponent>().TryTakeDamage(damage, damageType, attackRangeType);
    }
}
