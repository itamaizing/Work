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

        _playerTarget.GetComponent<HealthComponent>().OnTakePhisicDamage += DamageReduction;
        _playerTarget.GetComponent<HealthComponent>().OnTakeMagicDamage += DamageReduction;
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

    private void DamageReduction(HealthComponent.DamageInfo damageInfo)
    {
            damageInfo.ModifiedDamage -= damageInfo.ModifiedDamage * 0.1f;
    }

    private IEnumerator PoisonCoroutine(float duration)
    {
        float originSpeed = _playerTarget.GetComponent<CharacterData>().MoveSpeed;
        _playerTarget.GetComponent<MoveComponent>().ChangeMoveSpeed(0.1f);

        while (duration > 0)
        {
            _playerTarget.GetComponent<HealthComponent>().TryTakeDamage(_damageValue, DamageType.Physical, AttackRangeType.Inner);
            Player.GetComponent<PsionicaMelee>().MakePsionica(PsionicaValue);

            yield return new WaitForSeconds(1f);
            duration--;
        }
        _playerTarget.GetComponent<MoveComponent>().SetDefaultSpeed();

        Destroy(gameObject);
    }

    private void OnDisable()
    {
        _playerTarget.GetComponent<HealthComponent>().OnTakePhisicDamage -= DamageReduction;
        _playerTarget.GetComponent<HealthComponent>().OnTakeMagicDamage -= DamageReduction;

    }
}
