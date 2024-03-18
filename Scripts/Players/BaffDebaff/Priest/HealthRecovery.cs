using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthRecovery : BaseEffect
{
    public bool isRecovery;
    private float _recDuration;
    private float _recCooldown;
    private float _recHealth;
    public float Timer;

        public delegate void FourthBaffHandler(float value);
    public event FourthBaffHandler FourthBaffEvent;

    public void CastRecovery(float duration, float heal, float cooldown)
    {
        _recDuration = duration;
        _recCooldown = cooldown;
        _recHealth = heal;

    }
    private void Start()
    {
        Timer = Time.time;
        isRecovery = true;
        Type = EffectType.Buff;
    }
    private void Update()
    {
        if (isRecovery)
        {
            transform.parent.GetComponent<HealthPlayer>().AddHeal(_recHealth);
            FourthBaffEvent?.Invoke(_recHealth);
            if(transform.parent.GetComponent<HealthPlayer>().Health < 1000)
            {
                _recHealth += _recHealth * 0.1f / 4f;
                _recHealth = float.Parse(_recHealth.ToString("F2"));
            }

            StartCoroutine(Cooldown());
        }
        else if (Time.time >= _recDuration + Timer)
        {
            isRecovery = false;
            Destroy(this);
        }
    }
    private IEnumerator Cooldown()
    {
        isRecovery = false;
        yield return new WaitForSeconds(_recCooldown);
        isRecovery = true;

    }
}
