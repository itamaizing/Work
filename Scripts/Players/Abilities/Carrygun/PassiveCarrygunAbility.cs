using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static HealthPlayer;

public class PassiveCarrygunAbility : MonoBehaviour
{
    private float AvoidanceChance = 0.03f;
    private float _health;
    private float _maxHealth;
    private bool _isRegeneration;
    private HealthPlayer _healthPlayer;

    void Start()
    {
        GameObject _player = transform.parent.gameObject;
        _healthPlayer = _player.GetComponent<HealthPlayer>();

        _health = _healthPlayer.Health; 
        _maxHealth = _healthPlayer.MaxHealth;
        _isRegeneration = true;

        if (_healthPlayer != null)
        {
            _healthPlayer.OnTakePhisicDamage += HandleAvoidance;
            _healthPlayer.OnTakePhisicDamage += HandleProtection;

            _healthPlayer.OnTakeMagicDamage += HandleAvoidance;
            _healthPlayer.OnTakeMagicDamage += HandleProtection;

        }
    }

    void Update()
    {
        HandleHeal();
    }

    private void OnDestroy()
    {
        if (_healthPlayer != null)
        {
            _healthPlayer.OnTakePhisicDamage -= HandleAvoidance;
            _healthPlayer.OnTakePhisicDamage -= HandleProtection;

            _healthPlayer.OnTakeMagicDamage -= HandleAvoidance;
            _healthPlayer.OnTakeMagicDamage -= HandleProtection;
        }
    }
    private void HandleHeal()
    {
        if (_health < _maxHealth && _isRegeneration)
        {
            _healthPlayer.AddHeal(1.5f);
            StartCoroutine(HealthRegeneration());
        }
    }
    private void HandleAvoidance(HealthPlayer.DamageInfo damageInfo)
    {
        if (Random.Range(0f, 1f) <= AvoidanceChance)
        {
            damageInfo.ModifiedDamage = 0f;
        }
    }
    private void HandleProtection(HealthPlayer.DamageInfo damageInfo)
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
