using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BonePoison : BaseEffect
{
    [SerializeField] private int _baseDamage = 1;
    [SerializeField] private int _stackDuration = 30;
    private float _currentDamage;
    private float _timeBetweenAttack = 1.0f;
    private int _currentStacks = 1;
    private int _maxStacks = 4;

    private DamageType _damageType = DamageType.Physical;
    private AttackRangeType _attackRangeType = AttackRangeType.RangeAttack;

    private Coroutine _lifeTimeStacksCoroutine;
    private Coroutine _damageDealCoroutine;

    public int CurrentStacks => _currentStacks;
    public void AddStacks(HealthComponent targetHealth)
    {
        if (_currentStacks < _maxStacks)
        {
            _currentStacks++;

            if (_damageDealCoroutine == null)
            {
                _damageDealCoroutine = StartCoroutine(DamageDealCoroutine(targetHealth));
            }
            else
            {
                _currentDamage = _currentStacks * _baseDamage;
            }
        }
        _lifeTimeStacksCoroutine = StartCoroutine(LifeTimeStacksCoroutine(targetHealth));
    }

    private IEnumerator DamageDealCoroutine(HealthComponent targetHealth)
    {
        while (_currentStacks > 0)
        {
            //targetHealth.GetComponent<Character>().CharacterState.AddState(new AbilityFormDebuff(), 6.0f, 0, States.SchoolDebuff);
            targetHealth.TryTakeDamage(_currentDamage, _damageType, _attackRangeType);

            yield return new WaitForSeconds(_timeBetweenAttack);
        }
    }

    private IEnumerator LifeTimeStacksCoroutine(HealthComponent targetHealth)
    {
        while (_currentStacks > 0)
        {
            yield return new WaitForSeconds(_stackDuration);
            _currentStacks--;
            Debug.Log("CurrentStacks-- == " + _currentStacks);
        }

        if (_currentStacks == 0)
        {
            Debug.Log("else if coroutine");
            Destroy(gameObject);
            if (_damageDealCoroutine != null && _lifeTimeStacksCoroutine != null)
            {
                StopCoroutine(DamageDealCoroutine(targetHealth));
                _damageDealCoroutine = null;

                StopCoroutine(LifeTimeStacksCoroutine(targetHealth));
                _lifeTimeStacksCoroutine = null;
            }
        }
    }
}
