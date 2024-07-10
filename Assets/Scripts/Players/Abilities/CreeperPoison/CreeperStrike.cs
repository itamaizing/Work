using Mirror;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CreeperStrike : AutoAttackAbility
{
    [SerializeField] private LightningStrikes _lightningStrikes;
    [SerializeField] protected Character _dad;

    private BonePoison _bonePoisonDebuff;
    private GameObject _currentTarget;

    private float _currentDamage;
    private float _currentRadius;
    private float _multiplyCritDamage = 1.5f;

    private Coroutine _useAbilityCoroutine;
    private Coroutine _attackSpeedModifyCoroutine;
    private Coroutine _strikeCoroutine;

    public float CurrentAttackSpeed { get; set; }
    public float OriginalAttackSpeed { get; set; }
    protected float ThisRadius => _currentRadius;
    public GameObject CurrentTarget => _currentTarget;

    private new void Start()
    {
        _lightningStrikes = _dad.GetComponentInChildren<LightningStrikes>();
        OriginalAttackSpeed = _attackSpeed;
        CurrentAttackSpeed = OriginalAttackSpeed;
    }

    protected override void Cancel()
    {
        if (_useAbilityCoroutine != null)
            StopCoroutine(UseAbilityCoroutine());

        if (_strikeCoroutine != null)
            StopCoroutine(StrikeCoroutine(_currentDamage));
    }

    protected override void CastAction()
    {
        _currentDamage = Random.Range(7.0f, 11.0f);
        _useAbilityCoroutine = StartCoroutine(UseAbilityCoroutine());
    }

    public IEnumerator UseAbilityCoroutine()
    {
        _currentTarget = Target.gameObject;
        _strikeCoroutine = StartCoroutine(StrikeCoroutine(_currentDamage));
        yield return null;
    }

    private IEnumerator StrikeCoroutine(float currentDamage)
    {
        float chanceOfCriticalStrike = 0.5f;
        float numbersForChanceOfCriticalStrike = Random.Range(0.0f, 1.0f);

        if (numbersForChanceOfCriticalStrike <= chanceOfCriticalStrike)
        {
            CmdCriticalDamage(CurrentTarget, currentDamage);           
        }
        else
        {
            CmdApplyDamage(CurrentTarget, currentDamage, DamageType.Physical, AttackRangeType.MeleeAttack);
        }

        Cancel();
        yield return null;
    }

    private float CalculateCriticalDamage(float baseDamage)
    {
        float criticalDamage = baseDamage;
        float multiplyDamage = _multiplyCritDamage;

        if (_bonePoisonDebuff != null)
        {
            //Debug.Log("If Crit work");

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

    [Command]
    private void CmdCriticalDamage(GameObject currentTarget, float criticalDamage)
    {
        _bonePoisonDebuff = currentTarget.GetComponentInChildren<BonePoison>();
        if (_bonePoisonDebuff != null)
        {
            criticalDamage = CalculateCriticalDamage(criticalDamage);
        }
        currentTarget.GetComponent<HealthComponent>().TryTakeDamage(criticalDamage, DamageType.Physical, AttackRangeType.MeleeAttack);
    }
}
