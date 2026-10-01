using System.Collections.Generic;
using DG.Tweening;
using Mirror;
using System.Linq;
using UnityEngine;

public class DeathSpiralProjectile : Projectiles
{
    private Character _target;
    private float secondaryRadius = 3f;
    private float secondaryDamageMultiplier = 0.75f;
    private bool canTriggerSecondary = false;
    private float _value;
    private bool _isHeal;
    private bool _isSecondary;
    

    public void SetTarget(Character target) => _target = target;

    public void SetDamageOrHeal(float value, bool isHeal)
    {
        _value = value;
        _isHeal = isHeal;
    }

    public void SetAsSecondary(bool isSecondary,bool canTrigger)
    {
        _isSecondary = isSecondary;
        canTriggerSecondary = canTrigger;
    }
    
    private void Update()
    {
        if (_target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 direction = (_target.transform.position - transform.position).normalized;
        _rb.linearVelocity = direction * 5f;

        if (Vector3.Distance(transform.position, _target.transform.position) < 0.25f)
        {
            ApplyEffect();
            Destroy(gameObject);
        }
    }

    private void ApplyEffect()
    {
        if(!isServer) return;
        if (_isHeal && _target.Health != null)
        {
            _target.Health.Add(_value);
        }
        else if (!_isHeal && _target.Health != null)
        {
            Damage dmg = new Damage
            {
                Value = _value,
                Type = DamageType.Magical,
                School = Schools.Dark,
                DamageKey = "DeathSpiral"
            };
            _skill.ApplyDamage(dmg, _target.gameObject);
        }
        
        if (canTriggerSecondary && !_isSecondary)
        {
            (_skill as DeathSpiral)?.SpawnSecondaryParticles(_value,transform.position);
        }
    }
}
