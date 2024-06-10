using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepeatedDamage : BaseEffect
{
    public float MaxDamage;
    public float Duration;
    public bool IsRepeat = false;

    private float _summDamage;
    private float _damageDone;
    private float _damage;
    private float _modDamage;

    void Start()
    {
        StartCoroutine(TimeRepeat(Duration));
        Type = EffectType.Debuff;

    }

    public void RepeatDamage(ref float modifiedDamage)
    {
        _damage = modifiedDamage;
        _summDamage += _damage;

        if (_summDamage < MaxDamage)
        {
            _damageDone += _damage;

            modifiedDamage = _damage;
            _modDamage = modifiedDamage;
            IsRepeat = true;
            StartCoroutine(SecondDamage(0.2f));
        }
        else
        {

            modifiedDamage = _damage;
            _modDamage = MaxDamage - _damageDone;
            IsRepeat = true;
            StartCoroutine(SecondDamage(0.2f));
            _damageDone += _damage;
        }
    }
    private IEnumerator SecondDamage(float time)
    {
        yield return new WaitForSeconds(time);
        if(_modDamage > 0)
        {
            transform.parent.GetComponent<HealthComponent>().TryTakeDamage(_modDamage, DamageType.Magical, AttackRangeType.RangeAttack);
        }
        IsRepeat = false;
    }

    public IEnumerator TimeRepeat(float time)
    {

        yield return new WaitForSeconds(time);
        IsRepeat = false;
        Destroy(this);
    }
}
