using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpitPoisonProjectile : NetworkBehaviour
{
    [SerializeField] private Rigidbody2D _rb;
    [SerializeField] private GameObject _hitEffect;
    [SerializeField] private float _distance = 5f;
    [SerializeField] private int _force = 40;

    private Character _dad;
    private Vector2 _startPos;

    private float _energyDad;
    private float _damage;
    private float _lifeTimePoisonBoneStacks = 20.0f;

    private void Awake()
    {
        _startPos = transform.position;
        _rb.AddForce(transform.up * _force, ForceMode2D.Impulse);
    }

    private void FixedUpdate()
    {
        if (Vector2.Distance(transform.position, _startPos) > _distance * GlobalVariable.cellSize)
        {
            Explode();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.transform != _dad.transform)
        {
            if (collision.TryGetComponent<HeroComponent>(out var target))
            {
                _damage = Random.Range(4.0f, 12.0f);

                DealDamage(target, _damage, DamageType.Magical, AttackRangeType.RangeAttack);
            }
        }
    }

    private void DealDamage(HeroComponent target, float damage, DamageType damageType, AttackRangeType attackRangeType)
    {
        float chanceOfBlindness = 0.3f;
        float numbersForChanceOfBlindness = Random.Range(0.0f, 1.0f);

        Energy _energyLink = (Energy)_dad.Stamina;
        _energyLink.SumDamageMake(damage);

        target.Health.TryTakeDamage(damage, damageType, attackRangeType);

        PoisonBone.SetPlayer(_dad);
        target.CharacterState.CmdAddState(States.PoisonBone, _lifeTimePoisonBoneStacks, 0);

        if (numbersForChanceOfBlindness <= chanceOfBlindness)
        {
            //target.CharacterState.CmdAddState(States.Blind, 6f, 0);
        }

        Explode();
    }

    private void Explode()
    {
        Debug.Log("Ball is destroy in Explode()");
        if (_hitEffect != null)
        {
            GameObject hitEffect = Instantiate(_hitEffect, transform.position, Quaternion.identity);
            Destroy(hitEffect, 5f);
        }
        Destroy(gameObject);
    }

    public void InitializationProjectile(Character dad, float energy)
    {
        _dad = dad;
        _energyDad = energy;
    }
}
