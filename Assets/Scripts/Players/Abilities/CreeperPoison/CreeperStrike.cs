using Mirror;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CreeperStrike : AutoAttackSkill
{
    [Header("Talents")]
    [SerializeField] private StrokesOfAspiration _strokesOfAspiration;
    [SerializeField] private AssasinPoison _assasinPoison;
    [SerializeField] private DesireToHide _desireToHide;
    [SerializeField] private FirstStrike _firstStrike;

    [Header("Ability properties")]
    [SerializeField] protected Character _dad;
    [SerializeField] private CreeperInvisible _creeperInvisible;

    private Character _currentTarget;
    private Character _lastTarget;

    private int _currentCountHit = 0;
    private int _poisonBoneStacks = 0;
    private int _countHitForDesireToHideTalent = 0;

    private float _currentDamage;
    private float _multiplyCritDamage = 1.5f;
    private float _lifeTimePoisonBoneStacks = 6.0f;

    private float chanceOfCriticalStrike = 0.05f;
    
    private bool _isTwoHit = false;

    private Coroutine _useAbilityCoroutine;

    public int CurrentCountHit { get => _currentCountHit; set => _currentCountHit = value; }
    public int CountHitForReleaseFromSecrecyTalent 
    { 
        get => _countHitForDesireToHideTalent; 

        set => _countHitForDesireToHideTalent = value; 
    }
    public bool IsTwoHit { get => _isTwoHit; set => _isTwoHit = value; }

    public bool Enabled;
    public Character CurrentTarget => _currentTarget;

    protected override void ClearData()
    {
        base.ClearData();
        Debug.Log("CreeperStrike / ClearData");
        if (_firstStrike.IsActive)
        {
            _firstStrike.FirstHit = false;
        }

        _currentTarget = null;

        if (_useAbilityCoroutine != null)
        {
            StopCoroutine(UseAbilityCoroutine());
            _useAbilityCoroutine = null;
        }
    }

    protected override void CastAction()
    {
        Debug.Log("CreeperStrike / CastAction");

        _useAbilityCoroutine = StartCoroutine(UseAbilityCoroutine());
    }

    private IEnumerator UseAbilityCoroutine()
    {
        Debug.Log("CreeperStrike / UseAbilityCoroutine");
        _currentTarget = Target;
        Debug.Log($"CreeperStrike / UseAbilityCoroutine / CurrentTarget = {_currentTarget}");
        DealingDamageFromHits();
        yield return null;
    }

    public void DealingDamageFromHits()
    {
        Debug.Log("CreeperStrike / DealingDamageFromHits");
        _currentDamage = Random.Range(7.0f, 11.0f);
        float _currentChanceOfCriticalStrike = Random.Range(0.0f, 1.0f);

        _currentCountHit++;
        _countHitForDesireToHideTalent++;

        if (_currentCountHit == 2)
        {
            if (!_isTwoHit)
            {
                _isTwoHit = true;
            }
            _currentCountHit = 0;
        }

        if (_strokesOfAspiration.IsActive && _currentCountHit == 2)
        {
            if (_lastTarget == _currentTarget)
            {
                _strokesOfAspiration.UseTalentStrokesOfAspiration();
            }
            else
            {
                _lastTarget = _currentTarget;
            }
        }

        if (_assasinPoison.IsActive)
        {
            if (_assasinPoison.CurrentChargePoison > 0)
            {
                _assasinPoison.CmdSpendCharge(_currentTarget, _lifeTimePoisonBoneStacks);
            }
        }

        if (_desireToHide.IsActive)
        {
            if (_countHitForDesireToHideTalent == 5)
            {
                _desireToHide.IsCanApplyInvisible();
                _countHitForDesireToHideTalent = 0;
            }
        }

        if (_currentChanceOfCriticalStrike <= chanceOfCriticalStrike)
        {
            CmdCriticalDamage(CurrentTarget, _currentDamage);
        }
        else
        {
            CurrentTarget.Health.CmdTryTakeDamage(_currentDamage, DamageType.Physical, AttackRangeType.MeleeAttack);
        }
    }

    private float CalculateCriticalDamage(float baseDamage)
    {
        Debug.Log("CreeperStrike / CalculateCriticalDamage");
        float criticalDamage = baseDamage;
        float multiplyDamage = _multiplyCritDamage;
        float firstStrikeTalentMultiplyDamage = 5.0f;

        for (int i = 0; i < _poisonBoneStacks; i++)
        {
            multiplyDamage += 0.5f;
        }
        
        if (_firstStrike.IsActive && _firstStrike.IsCanIncreaseCrit && _firstStrike.FirstHit)
        {
            criticalDamage *= (multiplyDamage * firstStrikeTalentMultiplyDamage);
            _firstStrike.ReturnBoolFalse();
        }
        else
        {
            criticalDamage *= multiplyDamage;
        }
        return criticalDamage;
    }

    public void PoisonBoneStacks(int poisonBoneStacks)
    {
        _poisonBoneStacks = poisonBoneStacks;
    }

    private void CmdCriticalDamage(Character currentTarget, float criticalDamage)
    {
        Debug.Log("CreeperStrike / CmdCriticalDamage");
        if (currentTarget.CharacterState.CheckForState(States.PoisonBone))
        {
            criticalDamage = CalculateCriticalDamage(criticalDamage);
        }
        currentTarget.Health.CmdTryTakeDamage(criticalDamage, DamageType.Physical, AttackRangeType.MeleeAttack);
    }
}
