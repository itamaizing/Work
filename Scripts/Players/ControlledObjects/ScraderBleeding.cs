using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScraderBleeding : BaseEffect
{
    private bool _canMakeBleeding = false;
    public float BleedingDuration = 3;
    private float _valueCooldown = 1;
    private float _damageValue;
    [HideInInspector] public float Timer;

    void Start()
    {
        Timer = Time.time;
        Type = EffectType.Debuff;
        _canMakeBleeding = true;
    }

    private void Update()
    {
        if (_canMakeBleeding)
        {
            _damageValue = Random.Range(1, 3);

            transform.parent.GetComponent<HealthPlayer>().TakePhisicDamage(_damageValue);

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
