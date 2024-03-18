using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Poison : BaseEffect
{
    public GameObject Player;
    public float PsionicaValue;
    private GameObject _playerTarget;
    private Coroutine _coroutine;
    private float _damageValue;
    
    void Start()
    {
        _playerTarget = transform.parent.gameObject;
        Type = EffectType.Debuff;
        _damageValue = 1;
        PsionicaValue = _damageValue;

        _playerTarget.GetComponent<HealthPlayer>().OnTakePhisicDamage += DamageReduction;
        _playerTarget.GetComponent<HealthPlayer>().OnTakeMagicDamage += DamageReduction;
    }

    public void AddPoison(float duration)
    {
        if(_playerTarget == null)
        {
            _playerTarget = transform.parent.gameObject;
        }

        if (_coroutine == null)
        {
            _coroutine = StartCoroutine(PoisonCoroutine(duration));
        }
    }

    private void DamageReduction(HealthPlayer.DamageInfo damageInfo)
    {
            damageInfo.ModifiedDamage -= damageInfo.ModifiedDamage * 0.1f;
    }

    private IEnumerator PoisonCoroutine(float duration)
    {
        float originSpeed = _playerTarget.GetComponent<PlayerMove>().MoveSpeed;
        _playerTarget.GetComponent<PlayerMove>().MoveSpeed -= originSpeed * 0.1f;

        while (duration > 0)
        {
            _playerTarget.GetComponent<HealthPlayer>().TakePhisicDamage(_damageValue);
            Player.GetComponent<PsionicaMelee>().MakePsionica(PsionicaValue);

            yield return new WaitForSeconds(1f);
            duration--;
        }
        _playerTarget.GetComponent<PlayerMove>().MoveSpeed = originSpeed;

        Destroy(gameObject);
    }

    private void OnDisable()
    {
        _playerTarget.GetComponent<HealthPlayer>().OnTakePhisicDamage -= DamageReduction;
        _playerTarget.GetComponent<HealthPlayer>().OnTakeMagicDamage -= DamageReduction;

    }
}
