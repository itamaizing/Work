using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static HealthComponent;

public class PassiveCarrygunAbility : MonoBehaviour
{
    private float AvoidanceChance = 0.03f;
    private float _health;
    private float _maxHealth;
    private bool _isRegeneration;
    private HealthComponent _healthComponent;

    void Start()
    {
        GameObject _player = transform.parent.gameObject;
        _healthComponent = _player.GetComponent<HealthComponent>();

        //_health = _healthComponent._currentHealth; 
       // _maxHealth = _healthComponent._maxHealth;
        _isRegeneration = true;

        if (_healthComponent != null)
        {
            _healthComponent.OnTakePhisicDamage += HandleAvoidance;
            _healthComponent.OnTakePhisicDamage += HandleProtection;

            _healthComponent.OnTakeMagicDamage += HandleAvoidance;
            _healthComponent.OnTakeMagicDamage += HandleProtection;

        }
    }

    void Update()
    {
        HandleHeal();
    }

    private void OnDestroy()
    {
        if (_healthComponent != null)
        {
            _healthComponent.OnTakePhisicDamage -= HandleAvoidance;
            _healthComponent.OnTakePhisicDamage -= HandleProtection;

            _healthComponent.OnTakeMagicDamage -= HandleAvoidance;
            _healthComponent.OnTakeMagicDamage -= HandleProtection;
        }
    }
    private void HandleHeal()
    {
        if (_health < _maxHealth && _isRegeneration)
        {
            _healthComponent.AddHeal(1.5f);
            StartCoroutine(HealthRegeneration());
        }
    }
    private void HandleAvoidance(HealthComponent.DamageInfo damageInfo)
    {
        if (Random.Range(0f, 1f) <= AvoidanceChance)
        {
            damageInfo.ModifiedDamage = 0f;
        }
    }
    private void HandleProtection(HealthComponent.DamageInfo damageInfo)
    {
        damageInfo.ModifiedDamage *= 0.97f;
    }
    private IEnumerator HealthRegeneration()
    {
        _isRegeneration = false;
        yield return new WaitForSeconds(1f);

        _isRegeneration = true;
    }
}
