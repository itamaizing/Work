using Mirror;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CreeperStrike : AutoAttackAbility
{
    [SerializeField] private LightningStrikes _lightningStrikes;
    [SerializeField] private BonePoison _bonePoisonPrefab;
    [SerializeField] protected Character dad;
    [SerializeField] protected float _damageDeal = 0.0f;

    private BonePoison _bonePoisonDebuff;
    private GameObject _currentTarget;

    private float _currentDamage;
    private float _currentRadius;
    private float _multiplyCritDamage = 1.5f;

    public float CurrentAttackSpeed;
    public float OriginalAttackSpeed;

    private Coroutine _useAbilityCoroutine;
    private Coroutine _attackSpeedModifyCoroutine;
    private Coroutine _strikeCoroutine;

    protected float ThisRadius => _currentRadius;
    public float AttackSpeed => _attackSpeed;
    public GameObject CurrentTarget => _currentTarget;

    protected override void Cancel()
    {
        if (_useAbilityCoroutine != null)
            StopCoroutine(UseAbilityCoroutine());

        if (_attackSpeedModifyCoroutine != null)
            StopCoroutine(ModifyAttackSpeedCoroutine());

        if (_strikeCoroutine != null)
            StopCoroutine(StrikeCoroutine(Target, _currentDamage));
    }

    protected override void CastAction()
    {
        _currentDamage = Random.Range(7.0f, 11.0f);
        _useAbilityCoroutine = StartCoroutine(UseAbilityCoroutine());
    }

    public IEnumerator UseAbilityCoroutine()
    {
        _currentTarget = Target.gameObject;
        _strikeCoroutine = StartCoroutine(StrikeCoroutine(Target, _currentDamage));
        yield return null;
    }

    private IEnumerator StrikeCoroutine(Character enemy, float currentDamage)
    {
        float chanceOfCriticalStrike = 0.5f;
        float numbersForChanceOfCriticalStrike = Random.Range(0.0f, 1.0f);

        Debug.Log("Chance for Crit == " + numbersForChanceOfCriticalStrike);

        if (_lightningStrikes._isUsing)
        {
            _attackSpeedModifyCoroutine = StartCoroutine(ModifyAttackSpeedCoroutine());
        }

        if (numbersForChanceOfCriticalStrike <= chanceOfCriticalStrike)
        {
            _bonePoisonDebuff = CurrentTarget.GetComponentInChildren<BonePoison>();
            if (_bonePoisonDebuff != null)
            {
                currentDamage = CalculateCriticalDamage(currentDamage);
            }
        }

        Debug.Log("Strike Coroutine damage == " + currentDamage);
        //yield return CriticalDamageCoroutine(currentDamage);
        MakeDamage(enemy.Health, currentDamage, DamageType.Physical, AttackRangeType.MeleeAttack);

        yield return null;
    }

    private IEnumerator ModifyAttackSpeedCoroutine()
    {
        while (_lightningStrikes._isUsing)
        {
            _lightningStrikes.DecreaseAttackSpeed(CurrentAttackSpeed);

            yield return null;
        }
    }

    private float CalculateCriticalDamage(float baseDamage)
    {
        float criticalDamage = baseDamage;
        float multiplyDamage = _multiplyCritDamage;

        if (_bonePoisonDebuff != null)
        {
            Debug.Log("If Crit work");

            for (int i = 1; i < _bonePoisonDebuff.CurrentStacks; i++)
            {
                multiplyDamage += 0.5f;
            }
        }
        return criticalDamage *= multiplyDamage;
    }

    public void ModifyAttackSpeed(float attackSpeedStrikes)
    {
        CurrentAttackSpeed = attackSpeedStrikes;
    }

    public void ResetAttackSpeed()
    {
        CurrentAttackSpeed = OriginalAttackSpeed;
    }

    private void MakeDamage(HealthComponent target, float damage, DamageType damageType, AttackRangeType attackRangeType)
    {
        CmdApplyDamage(target.gameObject, damage, damageType, attackRangeType);
    }

    [Command]
    private void CmdApplyDamage(GameObject target, float damage, DamageType damageType, AttackRangeType attackRangeType)
    {
        target.GetComponent<HealthComponent>().TryTakeDamage(damage, damageType, attackRangeType);
    }
}
