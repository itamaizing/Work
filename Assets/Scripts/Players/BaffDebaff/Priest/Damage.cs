using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Damage : BaseEffect
{
    public bool isDamage;
    private float _damageDuration;
    private float _damageCooldown;
    private float _damageValue;
    public float Timer;

    public delegate void DarkFourthBaffHandler(float value);
    public event DarkFourthBaffHandler DarkFourthBaffEvent;

    public void CastRecovery(float duration, float damage, float cooldown)
    {
        _damageDuration = duration;
        _damageCooldown = cooldown;
        _damageValue = damage;
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
            transform.parent.GetComponent<HealthPlayer>().TakeMagicDamage(_damageValue);
            DarkFourthBaffEvent?.Invoke(_damageValue);

            StartCoroutine(Cooldown());
        }
        else if (Time.time >= _damageDuration + Timer)
        {
            isDamage = false;
            Destroy(this);
        }
    }
    private IEnumerator Cooldown()
    {
        isDamage = false;
        yield return new WaitForSeconds(_damageCooldown);
        isDamage = true;

    }
}
