using System;
using System.Collections;
using UnityEngine;

public class BleedingDebaff : BaseEffect
{
    private bool _canMakeBleeding = false;
    public float BleedingDuration = 3;
    private float _valueCooldown = 1;
    private float _damageValue = 6;
    private TwoMeleeAttack twoMeleeAttackComponent;
    [HideInInspector] public float Timer;

    void Start()
    {
        Timer = Time.time;
        Type = EffectType.Debuff;
        twoMeleeAttackComponent = FindObjectOfType<TwoMeleeAttack>();
        _canMakeBleeding = true;
    }

    private void Update()
    {
        if (_canMakeBleeding)
        {
            transform.parent.GetComponent<HealthPlayer>().TakePhisicDamage(_damageValue);

            if (twoMeleeAttackComponent != null && twoMeleeAttackComponent.gameObject != transform.parent.gameObject)
            {
                twoMeleeAttackComponent.gameObject.transform.parent.GetComponent<PsionicaMelee>().MakePsionica(_damageValue);
            }

            StartCoroutine(BleedingCooldown());
        
        }

        if (Time.time >= BleedingDuration + Timer)
        {
            _canMakeBleeding = false;
            Destroy(gameObject);
        }
    }

    private IEnumerator BleedingCooldown()
    {
        _canMakeBleeding = false;
        yield return new WaitForSeconds(_valueCooldown);
        _canMakeBleeding = true;
    }
}
