using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthRecovery : BaseEffect
{
    private GameObject Player;
    public bool isRecovery;
    private float _recDuration;
    private float _recCooldown;
    private float _recHealth;
    private float _defaultHealth;
    private float _time;
    private int _ticksCount;
    public float Timer
    {
        get
        {
            return _time;
        }
        set
        {
            _time = value;
        }
    }
    public int TicksCount
    {
        get
        {
            return _ticksCount;
        }
        set
        {
            _ticksCount = value;
        }
    }
    public float Health
    {
        get
        {
            return _recHealth;
        }
        set
        {
            _recHealth = value;
        }
    }

        public delegate void FourthBaffHandler(float value);
    public event FourthBaffHandler FourthBaffEvent;

    public void CastRecovery(float duration, float heal, float cooldown,GameObject player)
    {
        _recDuration = duration;
        _recCooldown = cooldown;
        _recHealth = heal;
        _defaultHealth = _recHealth;
        Player = player;
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
            StartCoroutine(Cooldown());
            AddTick();
            transform.parent.GetComponent<HealthPlayer>().AddHeal(_recHealth);
            FourthBaffEvent?.Invoke(_recHealth);
        }
        else if (Time.time >= _recDuration + Timer)
        {
            isRecovery = false;
            Invoke("DestroyThis", 0.1f);
        }
    }
    private void AddTick()
    {
        Debug.Log("Heal Before baffs - " + _recHealth);
        _ticksCount = Player.GetComponentInChildren<FourRangeRecovery>().CheckEneryOfSpriritBaffs();
        HealthPlayer hp = transform.parent.GetComponent<HealthPlayer>();

        if (transform.parent.GetComponentInChildren<DamageAbsorption>() != null)
        {
            _recHealth += _defaultHealth * 0.1f;
            Debug.Log("Shield Baff - "+ _defaultHealth * 0.1f +" Heal - " + _recHealth);
        }

        _defaultHealth = _recHealth;
        _recHealth += _ticksCount;
        Debug.Log("Iscra Baff - " + _ticksCount + "Heal -  " + _recHealth);

        float realHeal = hp.MaxHealth - hp.Health;
        if (realHeal <= _recHealth)
        {
            _recHealth = realHeal;
        }
        Debug.Log("Real Heal - " + _recHealth);
        if (_ticksCount > 0)
        Player.GetComponent<ManaPlayer>().AddMana(_recHealth * _ticksCount * 0.1f);
    }
    private void DestroyThis()
    {
        Destroy(this);
    }
    private IEnumerator Cooldown()
    {
        isRecovery = false;
        yield return new WaitForSeconds(_recCooldown);
        isRecovery = true;

    }
}
