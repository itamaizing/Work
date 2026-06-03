using System.Collections;
using UnityEngine;

public class LowHealthReductionPassive : Skill,IPassiveSkill
{
    private float _healthThresholdPercent = 80f;
    private float _damageReductionPercent = 80f;
    private int _hitsProtected = 3;
    private float _cooldown = 10f;

    protected override bool IsCanCast => false;
    
    private bool _isOnCooldown = false;
    private int _remainingHits = 0;
    private Coroutine _cooldownCoroutine;
    private bool _isEnabled;

    protected override IEnumerator CastJob()
    {
        yield return null;
    }

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    public void EnableHealthReduction(bool value)
    {
        if (_isEnabled == value) return;
        _isEnabled = value;

        CmdEnabledHealthReduction(_isEnabled);
    }
    
    private void CmdEnabledHealthReduction(bool value)
    {
        if (value)
        {
            _hero.Health.OnBeforeDamage += OnBeforeTakeDamage;
        }
        else
        {
            _hero.Health.OnBeforeDamage -= OnBeforeTakeDamage;

            if (_cooldownCoroutine != null)
                StopCoroutine(_cooldownCoroutine);

            _isOnCooldown = false;
            _remainingHits = 0;
        }
    }

    private void OnBeforeTakeDamage(ref Damage damage, Skill skill)
    {
        if (!_isOnCooldown && _remainingHits == 0)
        {
            float healthPercent = (_hero.Health.CurrentValue / _hero.Health.MaxValue) * 100f;

            if (healthPercent < _healthThresholdPercent)
            {
                ActivateProtection();
            }
        }
        
        if (_remainingHits > 0)
        {
            float reduction = _damageReductionPercent / 100f;
            damage.Value *= (1f - reduction);

            _remainingHits--;

            if (_remainingHits <= 0)
            {
                StartCooldown();
            }
        }
    }

    private void ActivateProtection()
    {
        _remainingHits = _hitsProtected;
    }

    private void StartCooldown()
    {
        if (_cooldownCoroutine != null)
            StopCoroutine(_cooldownCoroutine);

        _isOnCooldown = true;
        _cooldownCoroutine = StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        yield return new WaitForSeconds(_cooldown);

        _isOnCooldown = false;
        _remainingHits = 0;
    }

    private void OnDisable()
    {
        if (_hero.Health != null)
            _hero.Health.OnBeforeDamage -= OnBeforeTakeDamage;

        if (_cooldownCoroutine != null)
            StopCoroutine(_cooldownCoroutine);
    }
}
