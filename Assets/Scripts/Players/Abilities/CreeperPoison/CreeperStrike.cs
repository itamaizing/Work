using Mirror;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CreeperStrike : AutoAttackSkill
{
    public bool Enabled;

    [Header("Talents")]
    [SerializeField] private StrokesOfAspiration _strokesOfAspiration;
    [SerializeField] private AssasinPoison _assasinPoison;
    [SerializeField] private DesireToHide _desireToHide;
    [SerializeField] private FirstStrike _firstStrike;
    [SerializeField] private FeelingOfContinuation _feelingOfContinuation;
    [SerializeField] private PreparingForFight _preparingForFight;

    [Header("Ability properties")]
    [SerializeField] private Character _dad;
    [SerializeField] private CreeperInvisible _creeperInvisible;
    [SerializeField] private AbsoluteAccuracy _absoluteAccuracy;

    private Character _lastTarget;

    private int _currentCountHit = 0;
    private int _countHitForDesireToHideTalent = 0;
    private int _countCurrentHitForPreparingForFight = 0;

    private int _poisonBoneStacks = 0;

    private float _currentDamage;
    private float _multiplyCritDamage = 1.5f;
    private float _lifeTimePoisonBoneStacks = 6.0f;
    private float chanceOfCriticalStrike = 0.05f;
    
    private bool _isTwoHit = false;

    private Coroutine _useAbilityCoroutine;

    public int CurrentCountHit { get => _currentCountHit; set => _currentCountHit = value; }
    public int CountHitForReleaseFromSecrecyTalent { get => _countHitForDesireToHideTalent; set => _countHitForDesireToHideTalent = value; }
    public bool IsTwoHit { get => _isTwoHit; set => _isTwoHit = value; }
    public Character CurrentTarget => _target;

    protected override void ClearData()
    {
        base.ClearData();

        if (_useAbilityCoroutine != null)
        {
            StopCoroutine(UseAbilityCoroutine());
            _useAbilityCoroutine = null;
        }
    }

    protected override void CastAction()
    {
        Debug.Log("CastAction CreeperStrike");
        _useAbilityCoroutine = StartCoroutine(UseAbilityCoroutine());
    }

    private IEnumerator UseAbilityCoroutine()
    {
        Debug.Log("UseAbilityCoroutine CreeperStrike");
        DealingDamageFromHits(CurrentTarget);
        yield return null;
    }

    public void DealingDamageFromHits(Character target)
    {
        Debug.Log("DealDamage CreeperStrike");
        _currentDamage = Random.Range(7.0f, 11.0f);
        float _currentChanceOfCriticalStrike = Random.Range(0.0f, 1.0f);

        _currentCountHit++;

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
            if (_lastTarget == target)
            {
                _strokesOfAspiration.UseTalentStrokesOfAspiration();
            }
            else
            {
                _lastTarget = target;
            }
        }

        if (_assasinPoison.IsActive)
        {
            if (_assasinPoison.CurrentChargePoison > 0)
            {
                _assasinPoison.CmdSpendCharge(CurrentTarget, _lifeTimePoisonBoneStacks);
            }
        }

        if (_desireToHide.IsActive)
        {
            _countHitForDesireToHideTalent++;

            if (_countHitForDesireToHideTalent == 5)
            {
                _desireToHide.IsCanApplyInvisible();
                _countHitForDesireToHideTalent = 0;
            }
        }

        if (_preparingForFight.IsActive && _creeperInvisible.IsReadyToThreeHitForPreparingForFightTalent)
        {
            _countCurrentHitForPreparingForFight++;

            _preparingForFight.IncreaseManaRegeneration();

            if (_countCurrentHitForPreparingForFight == 3)
            {
                _countCurrentHitForPreparingForFight = 0;
                _creeperInvisible.IsReadyToThreeHitForPreparingForFightTalent = false;
            }
        }

        if (_absoluteAccuracy.IsCanCritCreeperStrike || _absoluteAccuracy.IsCanCritLightningStrikes)
        {
            Debug.Log("if absoluteAccuracy.IsCAnCrit == " + _absoluteAccuracy.IsCanCritCreeperStrike);
            DealCriticalDamage(target, _currentDamage);
        }
        else if (_currentChanceOfCriticalStrike <= chanceOfCriticalStrike)
        {
            Debug.Log("else if (_currentChanceOfCriticalStrike <= chanceOfCriticalStrike)");
            DealCriticalDamage(target, _currentDamage);
        }
        else
        {
            Damage damage = new Damage
            {
                Value = Buff.Damage.GetBuffedValue(_currentDamage),
                Type = DamageType.Physical,
                Range = AttackRangeType.MeleeAttack
            };

            CmdApplyDamage(damage, target.gameObject);
        }

        if (_firstStrike.IsActive)
        {
            _firstStrike.FirstHit = false;
        }
    }

    private float CalculateCriticalDamage(float baseDamage)
    {
        Debug.Log("CalculateCriticalDamage");
        float criticalDamage = baseDamage;
        float multiplyDamage = _multiplyCritDamage;
        float firstStrikeTalentMultiplyDamage = 5.0f;
        float absoluteAccucaryMultiplyDamage = 2.5f;

        if (_poisonBoneStacks > 0)
        {
            Debug.Log("CalculateCriticalDamage / poisonBoneStacks > 0");
            for (int i = 0; i < _poisonBoneStacks; i++)
            {
                multiplyDamage += 0.5f;
            }
        }

        if (_firstStrike.IsActive && _firstStrike.IsCanIncreaseCrit && _firstStrike.FirstHit)
        {
            Debug.Log("CalculateCriticalDamage / if (_firstStrike.IsActive && _firstStrike.IsCanIncreaseCrit && _firstStrike.FirstHit)");
            criticalDamage *= (multiplyDamage * firstStrikeTalentMultiplyDamage);
            _firstStrike.ReturnBoolFalse();
        }
        else if (_absoluteAccuracy.IsCanCritCreeperStrike && _poisonBoneStacks == 0)
        {
            Debug.Log("CalculateCriticalDamage / else if (_absoluteAccuracy.IsCanCrit && _poisonBoneStacks == 0)");
            criticalDamage *= absoluteAccucaryMultiplyDamage;

            if (_creeperInvisible.IsInvisible)
            {
                Debug.Log("CreeperStrike / CritDamage / IsInvisible == true");
                _creeperInvisible.ExitingInvisibleState();
            }
            Debug.Log("CalculateCriticalDamage / else if (_absoluteAccuracy.IsCanCrit && _poisonBoneStacks == 0) / damage = " + criticalDamage);
        }
        else
        {
            Debug.Log("CalculateCriticalDamage");
            criticalDamage *= multiplyDamage;
        }
        return criticalDamage;
    }

    public void PoisonBoneStacks(int poisonBoneStacks)
    {
        _poisonBoneStacks = poisonBoneStacks;
    }

    private void DealCriticalDamage(Character currentTarget, float criticalDamage)
    {
        Debug.Log("DealCriticalDamage");
        if (_absoluteAccuracy.IsCanCritCreeperStrike || _absoluteAccuracy.IsCanCritLightningStrikes)
        {
            Debug.Log("CreeperStrike / _isCanCritLightningStrikes = " + _absoluteAccuracy.IsCanCritLightningStrikes);
            criticalDamage = CalculateCriticalDamage(criticalDamage);
        }
        else if (currentTarget.CharacterState.CheckForState(States.PoisonBone))
        {
            criticalDamage = CalculateCriticalDamage(criticalDamage);
        }

        Damage critDamage = new Damage
        {
            Value = Buff.Damage.GetBuffedValue(criticalDamage),
            Type = DamageType.Physical,
            Range = AttackRangeType.MeleeAttack
        };

        CmdApplyDamage(critDamage, currentTarget.gameObject);

        if (_feelingOfContinuation.IsActive)
        { 
            _feelingOfContinuation.IncreaseRegenerationMana(criticalDamage);
        }
    }

}
