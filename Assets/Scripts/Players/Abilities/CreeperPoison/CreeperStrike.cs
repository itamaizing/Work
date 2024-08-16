using Mirror;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CreeperStrike : AutoAttackAbility
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

    protected override void Start()
    {
        base.Start();
    }

    protected override void Cancel()
    {
        if (_firstStrike.IsActive)
        {
            _firstStrike.FirstHit = false;
        }

        if (_useAbilityCoroutine != null)
            StopCoroutine(UseAbilityCoroutine());
    }

    protected override void CastAction()
    {
        _useAbilityCoroutine = StartCoroutine(UseAbilityCoroutine());
    }

    private IEnumerator UseAbilityCoroutine()
    {
        _currentTarget = Target;
        DealingDamageFromHits();
        yield return null;
    }

    public void DealingDamageFromHits()
    {
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
            CmdApplyDamage(CurrentTarget.gameObject, _currentDamage, DamageType.Physical, AttackRangeType.MeleeAttack);
        }

        Cancel();
    }

    private float CalculateCriticalDamage(float baseDamage)
    {
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
        if (currentTarget.CharacterState.CheckForState(States.PoisonBone))
        {
            criticalDamage = CalculateCriticalDamage(criticalDamage);
        }
        CmdApplyDamage(currentTarget.gameObject, criticalDamage, DamageType.Physical, AttackRangeType.MeleeAttack);
    }
}
