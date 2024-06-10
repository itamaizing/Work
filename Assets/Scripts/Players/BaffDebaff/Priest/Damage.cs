using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Damage : BaseEffect
{
    private GameObject _player;
    public bool isDamage;
    private float _damageDuration;
    private float _damageCooldown;
    private float _damageValue;
    public float Timer;
    private float _defaultDamage;

    private float _ticksCount;

    public delegate void DarkFourthBaffHandler(float value);
    public event DarkFourthBaffHandler DarkFourthBaffEvent;

    public void CastRecovery(float duration, float damage, float cooldown, GameObject player)
    {
        _damageDuration = duration;
        _damageCooldown = cooldown;
        _damageValue = damage;
        _defaultDamage = damage;
        _player = player;
    }
    private void Start()
    {
        Timer = Time.time;
        isDamage = true;
        Type = EffectType.Debuff;
    }

    private void Update()
    {
        if (isDamage)
        {
            StartCoroutine(Cooldown());
        }
        else if (Time.time >= _damageDuration + Timer)
        {
            isDamage = false;
            Destroy(this,0.1f);
        }
    }

    private void ChangeDamageValue()
    {
        _ticksCount = _player.GetComponentInChildren<FourRangeRecovery>().CheckSpriritStacks(false);
        _damageValue += _ticksCount;
    }

    private IEnumerator Cooldown()
    {
        isDamage = false;
        yield return new WaitForSeconds(_damageCooldown);
        ChangeDamageValue();
        transform.parent.GetComponent<HealthComponent>().TryTakeDamage(_damageValue, DamageType.Magical, AttackRangeType.RangeAttack);
        DarkFourthBaffEvent?.Invoke(_damageValue);
        _damageValue = _defaultDamage;
        isDamage = true;

    }
}
