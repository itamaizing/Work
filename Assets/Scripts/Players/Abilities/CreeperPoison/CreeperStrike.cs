using Mirror;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CreeperStrike : AutoAttackSkill
{
    public bool Enabled;

    [Header("Talents")]
    [SerializeField] private ReleaseFromSecrecy _releaseFromSecrecy;
    [SerializeField] private StrokesOfAspiration _strokesOfAspiration;
    [SerializeField] private AssasinPoison _assasinPoison;
    [SerializeField] private DesireToHide _desireToHide;
    [SerializeField] private FirstStrike _firstStrike;
    [SerializeField] private FeelingOfContinuation _feelingOfContinuation;
    [SerializeField] private PreparingForFight _preparingForFight;

    [Header("Abilities")]
    [SerializeField] private CreeperInvisible _creeperInvisible;
    [SerializeField] private ColdBlood _coldBlood;
    [SerializeField] private AbsorptionOfPoisons _absorptionOfPoisons;

    [Header("Ability properties")]
    [SerializeField] private Character _player;

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
    private bool _isHit = false;

    private Coroutine _useAbilityCoroutine;

    public int CurrentCountHit { get => _currentCountHit; set => _currentCountHit = value; }
    public int CountHitForReleaseFromSecrecyTalent { get => _countHitForDesireToHideTalent; set => _countHitForDesireToHideTalent = value; }
    public bool IsTwoHit { get => _isTwoHit; set => _isTwoHit = value; }
    public bool IsHit { get => _isHit; set => _isHit = value; }
    public Character CurrentTarget { get => _target; }

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
        var localPlayer = NetworkClient.connection.identity.GetComponent<UserNetworkSettings>();
        Debug.Log("UseAbilityCoroutine CreeperStrike / localPlayer = " + localPlayer);
        yield return null;
    }

    public void DealingDamageFromHits(Character target)
    {
        _currentDamage = Random.Range(7.0f, 11.0f);
        float _currentChanceOfCriticalStrike = Random.Range(0.0f, 1.0f);

        _isHit = true;
        _currentCountHit++;

        if (_currentCountHit == 2)
        {
            if (!_isTwoHit)
            {
                _isTwoHit = true;
            }
            _currentCountHit = 0;
        }

        if (_absorptionOfPoisons != null && _absorptionOfPoisons.IsWorking)
        {
            Debug.Log("DealDamage CreeperStrike / absorption.IsWorking");
            _absorptionOfPoisons.CheckTargetWithDebuffs(target.gameObject);
        }

        if (_strokesOfAspiration.Data.IsOpen && _currentCountHit == 2)
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

        if (_desireToHide.Data.IsOpen)
        {
            _countHitForDesireToHideTalent++;

            if (_countHitForDesireToHideTalent == 5)
            {
                _desireToHide.IsCanApplyInvisible();
                _countHitForDesireToHideTalent = 0;
            }
        }

        if (_releaseFromSecrecy.Data.IsOpen && _creeperInvisible.IsInvisible)
        {
            _creeperInvisible.ExitingInvisibleState();
        }

        if (_assasinPoison.Data.IsOpen)
        {
            if (_assasinPoison.CurrentChargeAssasinPoison > 0)
            {
                _assasinPoison.CmdSpendCharge(CurrentTarget, _lifeTimePoisonBoneStacks);
            }
        }


        if (_preparingForFight.Data.IsOpen && _creeperInvisible.IsReadyToThreeHitForPreparingForFightTalent)
        {
            _countCurrentHitForPreparingForFight++;

            _preparingForFight.IncreaseManaRegeneration();

            if (_countCurrentHitForPreparingForFight == 3)
            {
                _countCurrentHitForPreparingForFight = 0;
                _creeperInvisible.IsReadyToThreeHitForPreparingForFightTalent = false;
            }
        }

        //if (_coldBlood.IsCanCritCreeperStrike || _coldBlood.IsCanCritLightningStrikes)
        //{
        //    DealCriticalDamage(target, _currentDamage);
        //}
        if (_currentChanceOfCriticalStrike <= chanceOfCriticalStrike)
        {
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

        if (_firstStrike.Data.IsOpen)
        {
            _firstStrike.FirstHit = false;
        }

        _isHit = false;
    }

    private float CalculateCriticalDamage(Character target, float baseDamage)
    {
        float criticalDamage = baseDamage;
        float multiplyDamage = _multiplyCritDamage;
        float firstStrikeTalentMultiplyDamage = 5.0f;
        float absoluteAccucaryMultiplyDamage = 2.5f;

        if (_poisonBoneStacks > 0)
        {
            for (int i = 0; i < _poisonBoneStacks; i++)
            {
                multiplyDamage += 0.5f;
            }
        }

        if (_firstStrike.Data.IsOpen && _firstStrike.IsCanIncreaseCrit && _firstStrike.FirstHit)
        {
            criticalDamage *= (multiplyDamage * firstStrikeTalentMultiplyDamage);
            _firstStrike.ReturnBoolFalse();
        }
        /*
         * ITS NEEDED
        //else if (_coldBlood.IsCanCritCreeperStrike && _poisonBoneStacks == 0)
        //{
        //    if (!target.CharacterState.CheckPoisonStates())
        //    {
        //        _coldBlood.ReducingAbilityCooldown();
        //    }

        //    criticalDamage *= absoluteAccucaryMultiplyDamage;

        //    if (_creeperInvisible.IsInvisible)
        //    {
        //        _creeperInvisible.ExitingInvisibleState();
        //    }
        //    else
        //    {
        //        _coldBlood.IsCanCritCreeperStrike = false;
        //    }
        //}
        */
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

    private void DealCriticalDamage(Character currentTarget, float criticalDamage)
    {
        /*
         * ITS NEEDED
        //if (_coldBlood.IsCanCritCreeperStrike || _coldBlood.IsCanCritLightningStrikes)
        //{
        //    criticalDamage = CalculateCriticalDamage(currentTarget, criticalDamage);
        //}
        if (currentTarget.CharacterState.CheckForState(States.PoisonBone))
        {
            criticalDamage = CalculateCriticalDamage(currentTarget, criticalDamage);
        }
        */
        Damage critDamage = new Damage
        {
            Value = Buff.Damage.GetBuffedValue(criticalDamage),
            Type = DamageType.Physical,
            Range = AttackRangeType.MeleeAttack
        };

        CmdApplyDamage(critDamage, currentTarget.gameObject);

        if (_feelingOfContinuation.Data.IsOpen)
        { 
            _feelingOfContinuation.IncreaseRegenerationMana(criticalDamage);
        }
    }

}
