using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class CreeperStrike : Skill
{
    [Header("Dependencies")]
    [SerializeField] private Character _player;
    [SerializeField] private LightningStrikes _lightningStrikes;
    [SerializeField] private CreeperInvisible _creeperInvisible;
    [SerializeField] private ColdBlood _coldBlood;
    [SerializeField] private CreeperPoisonAura _creeperPoisonAura;
    [SerializeField] private CreeperCombo _creeperCombo;

    [Header("Damage")]
    [SerializeField] private float _minDamage = 7f;
    [SerializeField] private float _maxDamage = 11f;
    [SerializeField] private float _multiplyCritDamage = 1.5f;
    [SerializeField] private float _lifeTimePoisonBoneStacks = 6f;

    [Header("Targeting")]
    [SerializeField] private float _radiusSearchTarget = 0.5f;

    [Header("Reptile talent")]
    [SerializeField] private LayerMask _enemyLayer;
    [SerializeField] private float _poisonSearchRadius = 30f;
    [SerializeField] private float _baseIncreaseAttackSpeed = 0.1f;
    [SerializeField] private float _maxMinimumAttackSpeed = 0.1f;

    private Character _lastHitTarget;

    private float _currentDamage;
    private int _poisonBoneStack;

    private bool _isHit;
    private bool _isTwoHit;
    private bool _isSpeedOfReptileActive;
    private bool _isCheckForStatePoisonBone;

    private bool _isCreeperStrikeDamageAppliedThisCast;

    private Character _sneakySpitComboTarget;

    private bool _isNextHitFromLightningMovement;

    private Coroutine _sneakySpitComboResetCoroutine;
    private Coroutine _sneakySpitReadyCoroutine;

    private bool _isReptileTalentActive;
    private Coroutine _reptileCoroutine;

    private int _currentStacksPoison;
    private int _currentAllStacks;
    private int _previousAllStacks;
    private int _currentStacksAttackSpeed;

    private float _baseAttackSpeed;
    private float _currentAttackSpeedBonus;

    private PoisonBoneStateStacking _poisonBoneStateStacking;
    private EmpathicPoisonsState _empathicPoisonState;
    private WitheringPoisonStateStacking _witheringPoisonStateStacking;
    private BindingPoisonState _bindingPoisonState;

    protected override int AnimTriggerCast => Animator.StringToHash("CreeperStrikeAttacking");
    protected override int AnimTriggerPrepare => 0;

    public event Action OnCreeperStrikeEnd;
    public event Action OnHit;

    public Character LastHitTarget => _lastHitTarget;

    public int PoisonBoneStack
    {
        get => _poisonBoneStack;
        set => _poisonBoneStack = value;
    }

    public bool IsTwoHit
    {
        get => _isTwoHit;
        set => _isTwoHit = value;
    }

    public bool IsHit
    {
        get => _isHit;
        set => _isHit = value;
    }

    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render, hero);

        if (_player == null)
            _player = hero;
    }

    private void OnDisable()
    {
        StopReptileTalent();
    }

    #region Reptile13
    
    [SerializeField] private float _poisonCritChancePerStack = 0.05f;
    [SerializeField] private float _poisonCritBaseMultiplier = 2.5f;
    [SerializeField] private float _poisonCritBonusPerBoneStack = 1f;
    [SerializeField] private bool _countPoisonInRadius = false;
    [SerializeField] private float _poisonCritRadius = 6f;
    
    public void CheckForStatePoisonBone(bool value) => _isCheckForStatePoisonBone = value;
    
    private bool TryRollPoisonCrit(Character target, out float multiplier)
    {
        multiplier = 1f;
        if (!_isCheckForStatePoisonBone || target == null) return false;

        int total = 0, bone = 0;

        if (_countPoisonInRadius)
        {
            var enemies = Physics.OverlapSphere(transform.position, _poisonCritRadius, _enemyLayer);
            foreach (var enemy in enemies)
                if (enemy.TryGetComponent<CharacterState>(out var s))
                    CountPoisonStacks(s, ref total, ref bone);
        }
        else if (target.CharacterState != null)
        {
            CountPoisonStacks(target.CharacterState, ref total, ref bone);
        }

        if (total <= 0) return false;
        if (UnityEngine.Random.value > Mathf.Clamp01(total * _poisonCritChancePerStack)) return false;

        multiplier = _poisonCritBaseMultiplier + bone * _poisonCritBonusPerBoneStack;
        return true;
    }

    private static void CountPoisonStacks(CharacterState state, ref int total, ref int bone)
    {
        if (state.GetState(States.PoisonBone) is PoisonBoneStateStacking pb)
        {
            total += pb.CurrentStacks;
            bone += pb.CurrentStacks;
        }
        if (state.GetState(States.BindingPoison) is BindingPoisonState bp) total += bp.CurrentStacks;
        if (state.GetState(States.EmpathicPoisons) is EmpathicPoisonsState ep) total += ep.CurrentStacks;
        if (state.GetState(States.WitheringPoison) is WitheringPoisonStateStacking wp) total += wp.CurrentStacksCount;
    }
    
    #endregion
    
    public void SetSpeedOfReptile(bool value) => _isSpeedOfReptileActive = value;

    public void AnimCreeperStrikeCast()
    {
        if (_isCreeperStrikeDamageAppliedThisCast) return;
        _isCreeperStrikeDamageAppliedThisCast = true;

        AnimStartCastCoroutine();
    }

    public void MarkNextHitFromLightningMovement()
    {
        _isNextHitFromLightningMovement = true;
    }

    public void AnimCreeperStrikeEnded()
    {
        OnCreeperStrikeEnd?.Invoke();
        _isCreeperStrikeDamageAppliedThisCast = false;
        AnimCastEnded();
    }

    public void ClearDataCreeperStrike()
    {
        TryCancel();
        Renderer.HideSmartIndicator();
    }

    protected override IEnumerator CastJob()
    {
        if (Targeting.GetTarget() == null)
            yield break;
        Hero.Move.LookAtTransform(Targeting.GetTarget().Transform);

        bool isLightningMovementHit = _isNextHitFromLightningMovement;
        _isNextHitFromLightningMovement = false;

        DamageDeal(Targeting.GetTarget().Damageable, isLightningMovementHit);
        Hero.Move.StopLookAt();
        yield return null;
    }

    public void DamageDeal(IDamageable target, bool isUsingLightningStrikes = false)
    {
        if (target == null)
            return;

        Character character = target as Character;

        if (character == null)
            return;

        TryApplyPoisonBone(character);
        TryApplyWitheringPoison(character);
        TryConsumeAmbushPoison(character);

        _currentDamage = BaseDamage.GetValue();
        
        bool coldBloodCrit = CanDealColdBloodCriticalDamage();
        bool strikeEndsInvisibility = coldBloodCrit
                                      && _player != null && _player.IsInvisible
                                      && _creeperInvisible != null;

        TryApplyInvisibleCritBonus(strikeEndsInvisibility);

        _lastHitTarget = character;

        _isHit = true;
        OnHit?.Invoke();

        if (coldBloodCrit)
        {
            if (strikeEndsInvisibility)
                _creeperInvisible.ExitingInvisible();

            DealCriticalDamage(character, _currentDamage, true);
        }
        else if (TryRollPoisonCrit(character, out float critMultiplier))
        {
            Damage damage = GetDamage();
            damage.Value *= critMultiplier;
            CmdApplyDamage(damage, character.gameObject);
        }
        else
        {
            Damage damage = GetDamage();
            CmdDamageDeal(damage, character.gameObject);
        }

        if (_creeperCombo != null)
        {
            _creeperCombo.RegisterDamageToTarget(character);
        }

        _isHit = false;
    }

    private void TryApplyInvisibleCritBonus(bool strikeEndsInvisibility)
    {
        if (_creeperInvisible == null || !_creeperInvisible.IsInvisibilitStrike)
            return;

        if (!strikeEndsInvisibility && _creeperInvisible.StrikeCrit == 0)
            return;

        _currentDamage *= 4f;
        _creeperInvisible.StrikeCrit = 0;
    }

    private bool CanDealColdBloodCriticalDamage()
    {
        return _coldBlood != null
            && (_coldBlood.IsCanCrit || _coldBlood.IsCanCritLightningStrikes);
    }

    private bool CanDealPoisonBoneCriticalDamage(Character target)
    {
        return _isCheckForStatePoisonBone
            && target != null
            && target.CharacterState.CheckForState(States.PoisonBone);
    }

    private void TryApplyPoisonBone(Character target)
    {
        if (!_isSpeedOfReptileActive)
            return;

        if (target == null)
            return;

        target.CharacterState.CmdAddState(
            States.PoisonBone,
            _lifeTimePoisonBoneStacks,
            0,
            _player.gameObject,
            Name
        );
    }

    private void TryApplyWitheringPoison(Character target)
    {
        if (target == null) return;
        if (_creeperPoisonAura == null) return;

        CmdTryApplyWitheringPoison(target.gameObject);
    }

    private void TryConsumeAmbushPoison(Character target)
    {
        if (_hero.Abilities.GetSkill<AmbushPoisons>() == null) return;
        if (target == null) return;

        _hero.Abilities.GetSkill<AmbushPoisons>().TryConsumeStack(target);
    }

    [Command]
    private void CmdTryApplyWitheringPoison(GameObject targetObject)
    {
        if (targetObject == null) return;

        Character target = targetObject.GetComponent<Character>();
        if (target == null || target.CharacterState == null) return;

        if (_creeperPoisonAura == null) return;

        if (!_creeperPoisonAura.IsActiveWitheringPoison &&
            !_creeperPoisonAura.IsActiveWitheringPoisonMetabolism)
            return;

        float finalChance = 0f;

        if (_creeperPoisonAura.IsActiveWitheringPoison)
            finalChance += 0.2f;

        if (_creeperPoisonAura.IsActiveWitheringPoisonMetabolism)
            finalChance += 0.3f;

        if (UnityEngine.Random.value > finalChance)
            return;

        target.CharacterState.AddState(States.WitheringPoison, 10f, 0f, _player.gameObject, Name);
    }

    private void DealCriticalDamage(Character target, float baseDamage, bool isTalentCritDamage = false)
    {
        if (target == null)
            return;

        float criticalDamage = baseDamage;

        if (isTalentCritDamage) criticalDamage = CalculateCriticalDamage(baseDamage);

        Damage damage = GetDamage();
        damage.Value = criticalDamage;

        CmdApplyDamage(damage, target.gameObject);
    }

    private float CalculateCriticalDamage(float baseDamage)
    {
        float multiplier = _multiplyCritDamage;

        if (_poisonBoneStack > 0)
        {
            for (int i = 0; i < _poisonBoneStack; i++)
                multiplier += 0.5f;
        }

        if (_coldBlood != null && (_coldBlood.IsCanCrit || _coldBlood.IsCanCritLightningStrikes))
        {
            multiplier += 2.5f;

            if (_lightningStrikes != null && _lightningStrikes.IsUsedLightningStrikes)
            {
                _coldBlood.IsCanCrit = false;
            }
            else
            {
                _coldBlood.IsCanCrit = false;
                _coldBlood.IsCanCritLightningStrikes = false;
            }
        }

        return baseDamage * multiplier;
    }

    public void SetReptileTalentActive(bool value)
    {
        _isReptileTalentActive = value;

        if (value)
        {
            _baseAttackSpeed = PreparingDuration;

            if (_reptileCoroutine == null)
                _reptileCoroutine = StartCoroutine(ReptileLogic());
        }
        else
        {
            StopReptileTalent();
        }
    }

    private void StopReptileTalent()
    {
        _isReptileTalentActive = false;

        if (_reptileCoroutine != null)
        {
            StopCoroutine(_reptileCoroutine);
            _reptileCoroutine = null;
        }

        ResetAllAttackSpeed();
    }

    private IEnumerator ReptileLogic()
    {
        while (_isReptileTalentActive)
        {
            _currentStacksPoison = 0;
            _currentAllStacks = 0;

            Collider[] enemies = Physics.OverlapSphere(transform.position, _poisonSearchRadius, _enemyLayer);

            foreach (Collider enemy in enemies)
            {
                if (enemy == null)
                    continue;

                CharacterState state = enemy.GetComponent<CharacterState>();

                if (state == null)
                    continue;

                if (!state.Check(StatusEffect.Poison))
                    continue;

                CachePoisonStates(state);

                if (_bindingPoisonState != null)
                    _currentStacksPoison += _bindingPoisonState.CurrentStacks;

                if (_poisonBoneStateStacking != null)
                    _currentStacksPoison += _poisonBoneStateStacking.CurrentStacks;

                if (_empathicPoisonState != null)
                    _currentStacksPoison += _empathicPoisonState.CurrentStacks;

                if (_witheringPoisonStateStacking != null)
                    _currentStacksPoison += _witheringPoisonStateStacking.CurrentStacksCount;
            }

            _currentAllStacks = _currentStacksPoison;

            HandleAttackSpeed();

            yield return new WaitForSeconds(0.1f);
        }
    }

    private void CachePoisonStates(CharacterState state)
    {
        _bindingPoisonState = state.GetState(States.BindingPoison) as BindingPoisonState;
        _poisonBoneStateStacking = state.GetState(States.PoisonBone) as PoisonBoneStateStacking;
        _empathicPoisonState = state.GetState(States.EmpathicPoisons) as EmpathicPoisonsState;
        _witheringPoisonStateStacking = state.GetState(States.WitheringPoison) as WitheringPoisonStateStacking;
    }

    private void HandleAttackSpeed()
    {
        if (_currentAllStacks > _previousAllStacks)
        {
            while (_currentStacksAttackSpeed < _currentAllStacks)
            {
                if (PreparingDuration > _maxMinimumAttackSpeed)
                {
                    IncreaseAttackSpeed();
                }
                else
                {
                    break;
                }
            }

            _previousAllStacks = _currentAllStacks;
        }

        if (_currentAllStacks < _previousAllStacks)
        {
            while (_currentStacksAttackSpeed > _currentAllStacks)
            {
                ResetAttackSpeed();
            }

            _previousAllStacks = _currentAllStacks;
        }

        if (_currentAllStacks == 0 && _previousAllStacks != 0)
        {
            ResetAllAttackSpeed();
        }
    }

    private void IncreaseAttackSpeed()
    {
        _currentStacksAttackSpeed++;

        _currentAttackSpeedBonus = _baseIncreaseAttackSpeed;

        Buff.AttackSpeed.IncreasePercentage(_currentAttackSpeedBonus);
    }

    private void ResetAttackSpeed()
    {
        if (_currentStacksAttackSpeed <= 0)
            return;

        Buff.AttackSpeed.ReductionPercentage(_currentAttackSpeedBonus);

        _currentStacksAttackSpeed--;
    }

    private void ResetAllAttackSpeed()
    {
        while (_currentStacksAttackSpeed > 0)
        {
            ResetAttackSpeed();
        }

        _currentStacksPoison = 0;
        _currentAllStacks = 0;
        _previousAllStacks = 0;
    }

    protected override void ClearData()
    {
        _lastHitTarget = null;
        _isNextHitFromLightningMovement = false;

        Targeting.ClearTarget();
        Targeting.ClearTempTarget();

        Hero.Move.StopLookAt();
    }

    [Command]
    private void CmdDamageDeal(Damage damage, GameObject target)
    {
        ApplyDamage(damage, target);
    }
}