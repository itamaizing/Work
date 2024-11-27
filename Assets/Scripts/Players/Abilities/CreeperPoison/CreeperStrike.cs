using Mirror;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class CreeperStrike : AutoAttackSkill
{
    #region Variables

    [Header("Talents")]
    [SerializeField] private RestorationOfGlands _restorationOfGlands;
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
    [SerializeField] private LightningStrikes _lightningStrikes;
    //[SerializeField] private AbsorptionOfPoisons _absorptionOfPoisons;

    [Header("Ability properties")]
    [SerializeField] private Character _player;
    [SerializeField] private float _multiplyCritDamage = 1.5f;
    [SerializeField ]private float _chanceOfCriticalStrike = 0.05f;

    private Character _lastTarget;

    private int _currentCountHit = 0;
    private int _countHitForDesireToHideTalent = 0;
    private int _countCurrentHitForPreparingForFight = 0;
    private int _poisonBoneStack = 0;

    private float _currentDamage;
    private float _lifeTimePoisonBoneStacks = 6.0f;

    private bool _isTwoHit = false;
    private bool _isHit = false;

    private Coroutine _timerForTwoHitVariableCoroutine;

    public int CurrentCountHit { get => _currentCountHit; set => _currentCountHit = value; }
    public int CountHitForReleaseFromSecrecyTalent { get => _countHitForDesireToHideTalent; set => _countHitForDesireToHideTalent = value; }
    public int PoisonBoneStack { get => _poisonBoneStack; set => _poisonBoneStack = value; }
    public bool IsTwoHit { get => _isTwoHit; set => _isTwoHit = value; }
    public bool IsHit { get => _isHit; set => _isHit = value; }
    public Character CurrentTarget { get => _target; set => _target = value; }

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerAutoAttack => Animator.StringToHash("CreeperStrikeAttacking");

    #endregion

    #region CastAbility

    public void AnimCreeperStrikeCast()
    {
        AnimCastAction();
    }

    public void AnimCreeperStrikeEnded()
    {
        AnimCastEnded();
    }

    protected override void CastAction()
    {
        DamageDeal(CurrentTarget, false);
    }

    public void DamageDeal(Character target, bool isUsingLightningStrikes = false)
    {
        if (target != null)
        {
            _currentDamage = Random.Range(7.0f, 11.0f);
            float _currentChanceOfCriticalStrike = Random.Range(0.0f, 1.0f);

            _isHit = true;
            _currentCountHit++;

            if (_currentCountHit == 2 || isUsingLightningStrikes && _currentCountHit == 2)
            {
                float time = 10f;

                if (_timerForTwoHitVariableCoroutine != null)
                {
                    StopCoroutine(_timerForTwoHitVariableCoroutine);
                }

                _timerForTwoHitVariableCoroutine = StartCoroutine(TimerForTwoHit(time, isUsingLightningStrikes));
                
                _currentCountHit = 0;
            }

            //if (_absorptionOfPoisons != null && _absorptionOfPoisons.IsWorking)
            //{
            //    _absorptionOfPoisons.CheckTargetWithDebuffs(target.gameObject);
            //}

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

            if (_restorationOfGlands.Data.IsOpen && _poisonBoneStack > 0 && target.CharacterState.CheckForState(States.PoisonBone))
            {
                Debug.Log("CreeperStrike / if == true");
                float baseChanceOfRestorationOfGlands = 0.9f;
                float chanceOfRestorationOfGlands = baseChanceOfRestorationOfGlands * _poisonBoneStack;

                if (Random.Range(0f, 1f) <= chanceOfRestorationOfGlands)
                {
                    Debug.Log("CreeperStrike / restorationOfGlands");
                    _restorationOfGlands.ReductionCooldown();
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

                CmdPreparingForFight(_player.gameObject);

                if (_countCurrentHitForPreparingForFight == 3)
                {
                    _countCurrentHitForPreparingForFight = 0;
                    _creeperInvisible.IsReadyToThreeHitForPreparingForFightTalent = false;
                }
            }

            if (_coldBlood.IsCanCritCreeperStrike || _coldBlood.IsCanCritLightningStrikes)
            {
                DealCriticalDamage(target, _currentDamage);
            }
            else if (_currentChanceOfCriticalStrike <= _chanceOfCriticalStrike)
            {
                DealCriticalDamage(target, _currentDamage);
            }
            else
            {

                Damage damage = new Damage
                {
                    Value = Buff.Damage.GetBuffedValue(_currentDamage),
                    Type = DamageType.Physical,
                    PhysicAttackType = AttackRangeType.MeleeAttack,
                };

                CmdDamageDeal(damage, target.gameObject);
            }

            if (_firstStrike.Data.IsOpen)
            {
                _firstStrike.FirstHit = false;
            }

            _isHit = false;
        }
    }

    private IEnumerator TimerForTwoHit(float duration, bool isUsingLightningStrikes)
    {
        float time = duration;

        while (time > 0)
        {
            time -= Time.deltaTime;
            
            if (time <= 0)
            {
                if (!_isTwoHit)
                {
                    _isTwoHit = true;
                }
                else if (isUsingLightningStrikes)
                {
                    _lightningStrikes.IsUsedLightningStrikes = isUsingLightningStrikes;
                }
            }

            yield return null;
        }

        _isTwoHit = false;
        _lightningStrikes.IsUsedLightningStrikes = false;

        StopCoroutine(_timerForTwoHitVariableCoroutine);
        _timerForTwoHitVariableCoroutine = null;
    }
    #endregion

    #region CalculateCriticalDamage

    private float CalculateCriticalDamage(Character target, float baseDamage)
    {
        float criticalDamage = baseDamage;
        float multiplyDamage = _multiplyCritDamage;
        float firstStrikeTalentMultiplyDamage = 5.0f;
        float coldBloodMultiplyDamage = 2.5f;

        if (_poisonBoneStack > 0)
        {
            for (int i = 0; i < _poisonBoneStack; i++)
            {
                multiplyDamage += 0.5f;
            }
        }

        if (_firstStrike.Data.IsOpen && _firstStrike.IsCanIncreaseCrit && _firstStrike.FirstHit)
        {
            criticalDamage *= (multiplyDamage * firstStrikeTalentMultiplyDamage);
            _firstStrike.ReturnBoolFalse();
        }
        if (_coldBlood.IsCanCritCreeperStrike && _poisonBoneStack == 0)
        {
            if (target.CharacterState.Check(StatusEffect.Poison))
            {
                _coldBlood.ReducingAbilityCooldown();
            }

            criticalDamage *= coldBloodMultiplyDamage;

            if (_creeperInvisible.IsInvisible)
            {
                _creeperInvisible.ExitingInvisibleState();
            }

            _coldBlood.IsCanCritCreeperStrike = false;
        }
        else
        {
            criticalDamage *= multiplyDamage;
        }
        return criticalDamage;
    }

    private void DealCriticalDamage(Character currentTarget, float criticalDamage)
    {
        if (_coldBlood.IsCanCritCreeperStrike || _coldBlood.IsCanCritLightningStrikes)
        {
            criticalDamage = CalculateCriticalDamage(currentTarget, criticalDamage);
        }
         
        if (currentTarget.CharacterState.CheckForState(States.PoisonBone))
        {
            criticalDamage = CalculateCriticalDamage(currentTarget, criticalDamage);
        }
        
        Damage critDamage = new Damage
        {
            Value = Buff.Damage.GetBuffedValue(criticalDamage),
            Type = DamageType.Physical,
            PhysicAttackType = AttackRangeType.MeleeAttack,
        };

        CmdApplyDamage(critDamage, currentTarget.gameObject);

        if (_feelingOfContinuation.Data.IsOpen)
        {
            CmdFeelingOfContinuation(_player.gameObject, critDamage.Value);
        }
    }

    #endregion

    #region CommandMethods

    [Command]
    private void CmdFeelingOfContinuation(GameObject player, float criticalDamage)
    {
        Character playerCharacter = player.GetComponent<Character>();

        _feelingOfContinuation.IncreaseRegenerationMana(playerCharacter, criticalDamage);
    }

    [Command]
    private void CmdPreparingForFight(GameObject player)
    {
        Character playerCharacter = player.GetComponent<Character>();

        _preparingForFight.IncreaseManaRegeneration(playerCharacter);
    }

    [Command]
    private void CmdDamageDeal(Damage damage, GameObject target)
    {
        ApplyDamage(damage, target.gameObject);
    }

    #endregion

}
