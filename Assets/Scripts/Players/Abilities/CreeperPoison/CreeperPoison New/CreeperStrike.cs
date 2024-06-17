using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreeperStrike : AutoAttackAbility
{
    [SerializeField] LightningStrikes lightningStrikes;

    [SerializeField] public float _currentRadius;
    [SerializeField] public float _currentAttackSpeed;
    [SerializeField] protected float _damageDeal = 0.0f;
    [SerializeField] protected Character dad;
    public float _originalAttackSpeed;
    private bool _enabled = false;

    public Character _currentTarget;
    public float AttackSpeed => _attackSpeed;
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
        enemy.Health.TakeDamage(currentDamage, DamageType.Physical);
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
}
