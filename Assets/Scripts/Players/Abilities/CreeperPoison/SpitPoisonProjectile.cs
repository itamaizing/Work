using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpitPoisonProjectile : MonoBehaviour
{
    [SerializeField] private BonePoison _bonePoisonPrefab;
    [SerializeField] private Rigidbody2D _rb;
    [SerializeField] private GameObject _hitEffect;
    [SerializeField] private float _distance = 5f;
    [SerializeField] private int _force = 40;
    private BonePoison _bonePoisonDebuff;
    private Character _dad;
    private Vector2 _startPos;

    private float _energyDad;
    private float _damage;

    private void Awake()
    {
        _startPos = transform.position;
        _rb.AddForce(transform.up * _force, ForceMode2D.Impulse);
    }

    private void Update()
    {
        if (Vector2.Distance(transform.position, _startPos) > _distance * GlobalVariable.cellSize)
        {
            Explode();
        }
    }

    [Server]
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.transform != _dad.transform)
        {
            if (collision.TryGetComponent<HealthComponent>(out var targetHealth))
            {
                _damage = Random.Range(4.0f, 12.0f);

                DealDamage(targetHealth, _damage, DamageType.Magical, AttackRangeType.RangeAttack);
                BonePoisonDebuff(targetHealth);
            }
        }
    }

    private void DealDamage(HealthComponent targetHealth, float damage, DamageType damageType, AttackRangeType attackRangeType)
    {
        // Chance to apply Blindness
        float _chanceOfBlindness = 0.3f;
        float _numbersForChanceOfBlindness = Random.Range(0.0f, 1.0f);

        Energy _energyLink = (Energy)_dad.Stamina;
        _energyLink.SumDamageMake(damage);

        targetHealth.TryTakeDamage(damage, damageType, attackRangeType);

        if (_numbersForChanceOfBlindness <= _chanceOfBlindness)
        {
            targetHealth.GetComponent<Character>().CharacterState.AddState(new BlindnessState(), 3.0f, 0, States.Blind);
        }

        Explode();
    }

    private void BonePoisonDebuff(HealthComponent targetHealth)
    {
        _bonePoisonDebuff = targetHealth.GetComponentInChildren<BonePoison>();
        if (_bonePoisonDebuff == null)
        {
            _bonePoisonDebuff = Instantiate(_bonePoisonPrefab, targetHealth.transform);
            _bonePoisonDebuff.AddStacks(targetHealth);
        }
        else
        {
            _bonePoisonDebuff.AddStacks(targetHealth);
        }
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
