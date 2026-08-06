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

    private Character _castTarget;
    private Character _lastHitTarget;

    private float _currentDamage;
    private int _poisonBoneStack;

    private bool _isHit;
    private bool _isTwoHit;
    private bool _isSpeedOfReptileActive;
    private bool _isColdBloodStrike;
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

    private PoisonBoneState _poisonBoneState;
    private EmpathicPoisonsState _empathicPoisonState;
    private WitheringPoisonState _witheringPoisonState;
    private BindingPoisonState _bindingPoisonState;

    protected override int AnimTriggerCast => Animator.StringToHash("CreeperStrikeAttacking");
    protected override int AnimTriggerCastDelay => 0;
    protected override bool IsCanCast => CheckIsCanCast();

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

    public void CheckForStatePoisonBone(bool value) => _isCheckForStatePoisonBone = value;

    public void ColdBloodStrike(bool value)
    {
        if (value == _isColdBloodStrike) return;
            
        _isColdBloodStrike = value;
    }
    public void SetSpeedOfReptile(bool value) => _isSpeedOfReptileActive = value;

    public void AnimCreeperStrikeCast()
    {
        if (_castTarget == null) return;
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

    private bool CheckIsCanCast()
    {
        Character target = GetTargetForCurrentCastCheck();

        if (target == null) return false;
        return Vector3.Distance(target.transform.position, transform.position) <= AreaInfo.Radius && Targeting.NoObstacles(target.transform.position, transform.position, _obstacle);
    }

    private Character GetTargetForCurrentCastCheck()
    {
        if (IsCasting) return _castTarget;

        if (TargetInfoQueue.TryPeek(out TargetInfo queuedTargetInfo))
        {
            if (queuedTargetInfo.GetTargets().Count > 0) return queuedTargetInfo.GetTargets()[0] as Character;
        }

        return Targeting.GetTarget()?.Character;
    }

    private bool IsAllyTarget(IDamageable target)
    {
        return target.gameObject.layer == LayerMask.NameToLayer("Allies");
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();

        Targeting.ClearTempTarget();

        while (Targeting.GetTempTarget()?.Targetable == null)
        {
            if (GetMouseButton)
            {
                Targeting.FindTempTarget(Targeting.GetMousePoint(), _radiusSearchTarget);

                if (Targeting.GetTempTarget()?.Targetable is IDamageable damageable)
                {
                    Character character = damageable as Character;

                    if (IsAllyTarget(damageable) || character == Hero)
                    {
                        Targeting.ClearTempTarget();
                    }
                    else
                    {
                        break;
                    }
                }
            }

            yield return null;
        }

        TargetData preparedTarget = Targeting.GetTempTarget();

        if (preparedTarget == null || preparedTarget.Targetable == null) yield break;
        if (!IsCasting) Targeting.SetTarget(preparedTarget.Targetable);

        targetInfo.Points.Add(preparedTarget.Transform.position);
        targetInfo.AddTarget(preparedTarget.Targetable);

        callbackDataSaved?.Invoke(targetInfo);
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        _castTarget = null;

        if (targetInfo == null)
            return;

        if (targetInfo.GetTargets().Count == 0)
            return;

        _castTarget = targetInfo.GetTargets()[0] as Character;

        if (_castTarget == null)
            return;

        Targeting.SetTarget(_castTarget);
        Hero.Move.LookAtTransform(_castTarget.transform);
    }

    protected override IEnumerator CastJob()
    {
        if (_castTarget == null)
            yield break;

        Hero.Move.StopLookAt();

        bool isLightningMovementHit = _isNextHitFromLightningMovement;
        _isNextHitFromLightningMovement = false;

        DamageDeal(_castTarget, isLightningMovementHit);

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

        _currentDamage = UnityEngine.Random.Range(_minDamage, _maxDamage);

        TryApplyInvisibleCritBonus();

        _lastHitTarget = character;

        _isHit = true;
        OnHit?.Invoke();

        //TryReduceColdBloodCooldown(character);

        if (CanDealColdBloodCriticalDamage())
        {
            if (_player != null && _player.IsInvisible && _creeperInvisible != null)
                _creeperInvisible.ExitingInvisible();

            DealCriticalDamage(character, _currentDamage, true);
        }
        else if (CanDealPoisonBoneCriticalDamage(character))
        {
            DealCriticalDamage(character, _currentDamage);
        }
        else
        {
            Damage damage = new Damage
            {
                Value = Buff.Damage.GetBuffedValue(_currentDamage),
                Type = DamageType.Physical,
                PhysicAttackType = AttackRangeType.MeleeAttack,
            };

            CmdDamageDeal(damage, character.gameObject);
        }

        if (_creeperCombo != null)
        {
            _creeperCombo.RegisterDamageToTarget(character);
        }

        _isHit = false;
    }

    private void TryApplyInvisibleCritBonus()
    {
        if (_creeperInvisible == null)
            return;

        if (!_creeperInvisible.IsInvisibilitStrike)
            return;

        if (_creeperInvisible.StrikeCrit == 0)
            return;

        _currentDamage *= 4f;
        _creeperInvisible.StrikeCrit = 0;

        Debug.Log("CreeperStrike: invisible crit x4 applied");
    }

    private void TryReduceColdBloodCooldown(Character target)
    {
        if (!_isColdBloodStrike)
            return;

        if (_coldBlood == null)
            return;

        if (!_coldBlood.IsCanCrit)
            return;

        if (target == null)
            return;

        if (!target.CharacterState.CheckForState(States.Blind))
            return;

        _coldBlood.ReducingAbilityCooldown();
        _isColdBloodStrike = false;

        Debug.Log("CreeperStrike: ColdBlood cooldown reduced");
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

        target.CharacterState.AddState(
            States.WitheringPoison,
            10f,
            0f,
            gameObject,
            Name
        );
    }

    private void DealCriticalDamage(Character target, float baseDamage, bool isTalentCritDamage = false)
    {
        if (target == null)
            return;

        float criticalDamage = baseDamage;

        if (isTalentCritDamage || CanDealPoisonBoneCriticalDamage(target))
            criticalDamage = CalculateCriticalDamage(baseDamage);

        Damage damage = new Damage
        {
            Value = Buff.Damage.GetBuffedValue(criticalDamage),
            Type = DamageType.Physical,
            PhysicAttackType = AttackRangeType.MeleeAttack,
        };

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
            _baseAttackSpeed = CastDeley;

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

                if (_poisonBoneState != null)
                    _currentStacksPoison += _poisonBoneState.CurrentStacks;

                if (_empathicPoisonState != null)
                    _currentStacksPoison += _empathicPoisonState.CurrentStacks;

                if (_witheringPoisonState != null)
                    _currentStacksPoison += _witheringPoisonState.CurrentStacksCount;
            }

            _currentAllStacks = _currentStacksPoison;

            HandleAttackSpeed();

            yield return new WaitForSeconds(0.1f);
        }
    }

    private void CachePoisonStates(CharacterState state)
    {
        _bindingPoisonState = state.GetState(States.BindingPoison) as BindingPoisonState;
        _poisonBoneState = state.GetState(States.PoisonBone) as PoisonBoneState;
        _empathicPoisonState = state.GetState(States.EmpathicPoisons) as EmpathicPoisonsState;
        _witheringPoisonState = state.GetState(States.WitheringPoison) as WitheringPoisonState;
    }

    private void HandleAttackSpeed()
    {
        if (_currentAllStacks > _previousAllStacks)
        {
            while (_currentStacksAttackSpeed < _currentAllStacks)
            {
                if (CastDeley > _maxMinimumAttackSpeed)
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
        _castTarget = null;
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