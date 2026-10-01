using DG.Tweening;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockOfIceProjectile : Projectiles
{
    private Vector3 _startPos;
    private Damage _damage;
    private float _curDamage;
    private float _maxDistance = 6;
    
    public override void Init(Character dad, float energy, bool lastHit, Skill skill)
    {
        base.Init(dad, energy, lastHit, skill);

        _curDamage = energy;
        _damage = new Damage
        {
            Value = _curDamage,
            Type = DamageType.Magical,
        };

        _startPos = transform.position;
    }
    
    public void SetMaxDistance(float distance)
    {
        _maxDistance = distance;
    }

    private void Update()
    {
        if (!_initialized) return;
        
        if (Vector3.Distance(transform.position, _startPos) > _maxDistance)
            Explode();
    }

    [Server]
    private void OnTriggerEnter(Collider collision)
    {
        if (!_initialized || _dad == null) return;
        if (collision.gameObject == _dad.gameObject || collision.CompareTag("Ability")) return;

        if (!collision.TryGetComponent<IDamageable>(out var damageable)) return;

        if (collision.TryGetComponent<Character>(out var target))
        {
            if (target.CharacterState.CheckForState(States.Frozen))
                _curDamage *= 1.4f;

            _damage.Value = _curDamage;

            TargetRpcDamageMake(_curDamage);
            _skill.ApplyDamage(_damage, target.gameObject);

            _dad.Abilities.GetSkill<FrostEnergy>()
                ?.ApplyFrostEnergyStateBonus(target, States.Cooling, _skill);

            target.CharacterState.AddState(States.Cooling, 9f, 0, _dad.gameObject, _skill.name);

            if (_skill is BlockOfIce blockOfIce)
                blockOfIce.RegisterSeriesHit(target.gameObject);

            GetComponent<Collider>().enabled = false;
        }
        else
        {
            _skill.ApplyDamage(_damage, damageable.gameObject);
        }

        Explode();
    }

    private void Explode()
    {
        if (_hitEffect != null)
        {
            GameObject hitEffect = Instantiate(_hitEffect, transform.position, Quaternion.identity);
            Destroy(hitEffect, 5f);
        }
        Destroy(gameObject);
    }
}
