using Mirror;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public class ClawStrike : Skill
{
    [SerializeField] private Character _player;
    [SerializeField] private RechargeGlands _rechargeGlands;
    [SerializeField] private BasePsionicEnergy _basePsionicEnergy;
    [SerializeField] private AttackingPsionicEnergy _attackingPsionicEnergy;
    [SerializeField] private JumpWithChelicera _jumpWithChelicera;
    [SerializeField] private JumpBack _jumpBack;
    [SerializeField] private float _animSpeed = 0.8f;
    [SerializeField] private float _chanceApplyBleeding = 0.15f;
    [SerializeField] private float _durationBleeding = 7f;
    [SerializeField] private float _buffDurationAfterJump = 1f;
    [SerializeField] private float _chanceApplyBleedingIncrease = 0.4f;
    [SerializeField] private float _chanceApplyBleedingWithJump = 0.6f;

    [Header("Damage")]
    [SerializeField] private float _minDamage = 10f;
    [SerializeField] private float _maxDamage = 11f;

    #region Constants

    private const float AnimationSpeedDefault = 1f;
    private const float SpeedBonusMultiplier = 1.4f;

    private const float RandomChanceMin = 0f;
    private const float RandomChanceMax = 1f;
    private const float TryApplyDestructivePoisonChance = 0.5f;

    private const float PsiDispel_3 = 30f;
    private const float PsiDispel_2 = 20f;
    private const float PsiDispel_1 = 10f;

    private const float CheliceraBonusChance = 0.15f;
    private const float JumpWithCheliceraBonusChance = 0.45f;

    private const float TargetSearchRadius = 0.5f;

    #endregion

    private bool _isDurationChanceApplyBleedingWithJump = false;
    private bool _isLastClawStrike;
    private float _spentAttackingPsiEnergy;
    private float _baseDamage;
    private float _castWindowDuration = 1f;
    private float _totalChanceApplyBleeding;
    private Coroutine coroutineDurationChanceApplyBleedingWithJump;
    private WaitForSeconds _waitForBuffDuration;

    private const float JumpBackWindow = 1.5f;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("ClawStrikeTrigger");
    protected override bool IsCanCast => CheckIsCanCast();
    private AttributeModifier _speedBonusModifier;
    private bool IsAllyTarget(IDamageable target) => target.gameObject.layer == LayerMask.NameToLayer("Allies");

    public float CastWindowDuration { get => _castWindowDuration; set => _castWindowDuration = value; }

    private bool CheckIsCanCast()
    {
        return Targeting.GetTarget() != null &&
            Vector3.Distance(Targeting.GetTarget().Transform.position, transform.position) <= AreaInfo.Radius &&
            Targeting.NoObstacles(Targeting.GetTarget().Transform.position, transform.position, _obstacle);
    }

    private void OnDisable()
    {
        OnSkillCanceled -= HandleSkillCanceled;
    }

    private void OnEnable()
    {
        OnSkillCanceled += HandleSkillCanceled;
    }

    private void Start()
    {
        _waitForBuffDuration = new WaitForSeconds(_buffDurationAfterJump);
    }

    #region Talent
    private bool _isBleedingClawStrike = false;
    private bool _isChanceApplyBleedingIncrease = false;

    private bool _isClawStrikeComboTalentActive = false;
    private bool _wasCurrentCastBoosted;

    public void ClawStrikeSpeed(bool value)
    {
        _isClawStrikeComboTalentActive = value;
    }

    public void BleedingClawStrike(bool value) => _isBleedingClawStrike = value;
    public void ChanceApplyBleedingIncrease(bool value) => _isChanceApplyBleedingIncrease = value;
    #endregion
    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0) Targeting.SetTarget(targetInfo.GetTargets()[0]);
    }

    protected override IEnumerator CastJob()
    {
        if (Targeting.GetTarget() == null) yield break;
        if (!IsTargetInRange()) yield break;

        Character currentTarget = Targeting.GetTarget()?.Targetable as Character;

        TryActivateJumpBack(currentTarget);

        DamageDeal(currentTarget);

        ComboContext.JumpBack.LastTarget = currentTarget;
        ComboContext.JumpBack.LastSkill = typeof(ClawStrike);
        ComboContext.JumpBack.LastTime = Time.time;

        ComboContext.Bleeding.Set(typeof(ClawStrike));

        ComboContext.ClawStrikeContext.Set(typeof(ClawStrike), _wasCurrentCastBoosted);

        yield return null;
    }

    private bool IsTargetInRange() { return Targeting.GetTarget() != null && Vector3.Distance(_player.transform.position, Targeting.GetTarget().Transform.position) <= AreaInfo.Radius; }

    private void DamageDeal(ITargetable target)
    {
        if (target == null) return;

        IDamageable damageable = target as IDamageable;
        Character targetCharacter = target as Character;
        _baseDamage = UnityEngine.Random.Range(_minDamage, _maxDamage);
        Damage = _baseDamage;

        var physicalDamage = new Damage
        {
            Value = _baseDamage,
            Type = DamageType.Physical,
            PhysicAttackType = AttackRangeType.MeleeAttack,
        };

        CmdApplyDamage(physicalDamage, damageable.gameObject);

        if (_rechargeGlands != null && targetCharacter != null) _rechargeGlands.TryApplyDestructivePoison(targetCharacter, TryApplyDestructivePoisonChance, _player);

        if (targetCharacter != null) TryApplyBleeding(targetCharacter);

        if (_spentAttackingPsiEnergy > 0 && targetCharacter != null)
        {
            float psi = _spentAttackingPsiEnergy;
            var psiMagicDamage = new Damage
            {
                Value = psi,
                Type = DamageType.Magical,
                PhysicAttackType = AttackRangeType.MeleeAttack,
                School = Schools.Air,
                Form = AbilityForm.Magic,
            };

            CmdApplyDamage(psiMagicDamage, targetCharacter.gameObject);

            int dispelCount = Mathf.FloorToInt(psi / 10f);
            if (dispelCount > 0) CmdDispel(targetCharacter, dispelCount);
        }
    }

    private void TryActivateJumpBack(Character currentTarget)
    {
        if (_jumpBack == null) return;
        if (currentTarget == null) return;

        if (ComboContext.JumpBack.LastTarget != currentTarget)
        {
            ComboContext.JumpBack.Reset();
            return;
        }

        bool validPrevious =
            ComboContext.JumpBack.LastSkill == typeof(ClawStrike) ||
            ComboContext.JumpBack.LastSkill == typeof(CheliceraStrike);

        bool inWindow =
            Time.time - ComboContext.JumpBack.LastTime <= JumpBackWindow;

        if (validPrevious && inWindow)
        {
            _jumpBack.EnableJumpBack();
        }
        else
        {
            ComboContext.JumpBack.Reset();
        }
    }
    
    private void TryApplyBleeding(Character target)
    {
        if (!_isBleedingClawStrike) return;

        _totalChanceApplyBleeding = _chanceApplyBleeding;

        Type lastSkill = ComboContext.Bleeding.IsRecent ? ComboContext.Bleeding.LastSkill : null;

        if (lastSkill == typeof(CheliceraStrike)) _totalChanceApplyBleeding += CheliceraBonusChance;
        if (lastSkill == typeof(JumpWithChelicera)) _totalChanceApplyBleeding += JumpWithCheliceraBonusChance;

        if (_isDurationChanceApplyBleedingWithJump && _jumpWithChelicera.IsCheliceraStrikeCast && lastSkill == typeof(CheliceraStrike))
            _totalChanceApplyBleeding = _chanceApplyBleedingWithJump;

        if (_isChanceApplyBleedingIncrease && CheckStateForBleeding(target)) _totalChanceApplyBleeding += _chanceApplyBleedingIncrease;
        _totalChanceApplyBleeding = Mathf.Clamp01(_totalChanceApplyBleeding);

        Debug.Log($"_totalChanceApplyBleeding: {_totalChanceApplyBleeding}");
        
        float rand = UnityEngine.Random.Range(RandomChanceMin, RandomChanceMax);
        if (rand <= 100) CmdAddBleeding(target);

        _jumpWithChelicera.IsCheliceraStrikeCast = false;
        _isDurationChanceApplyBleedingWithJump = false;

        if (coroutineDurationChanceApplyBleedingWithJump != null) StopCoroutine(coroutineDurationChanceApplyBleedingWithJump);
    }

    protected override void PlayCastAnim()
    {
        RemoveSpeedModifier();

        _wasCurrentCastBoosted = _isClawStrikeComboTalentActive && ComboContext.ClawStrikeContext.IsValidPreviousSkill();

        if (_wasCurrentCastBoosted)
        {
            _speedBonusModifier = new AttributeModifier(SpeedBonusMultiplier, ModifierType.Multiplier, this);
            _hero.AttributeSystem[CharacterAttributeName.CastSpeedPhysical].AddModifier(_speedBonusModifier);
        }

        
        float currentCastSpeed = GetCastSpeed();
        _player.Animator.SetFloat(HashAnimPlayer.CastSpeed, currentCastSpeed);

        _hero.Animator.SetTrigger(AnimTriggerCast);
        _hero.NetworkAnimator.SetTrigger(AnimTriggerCast);
    }

    public void ClawStrikePreparingForAnim()
    {
        if (_attackingPsionicEnergy.IsAttackingPsiEnergy && _attackingPsionicEnergy.CurrentValue > 0f)
            TrySpendAttackingPsi();
        else
            _spentAttackingPsiEnergy = 0;
    }

    public void ClawStrikeCast()
    {
        AnimStartCastCoroutine();
    }

    public void ClawStrikeEnded()
    {
        AnimCastEnded();
    }
    
    private void RemoveSpeedModifier()
    {
        if (_speedBonusModifier != null)
        {
            _hero.AttributeSystem[CharacterAttributeName.CastSpeedPhysical].RemoveModifier(_speedBonusModifier);
            _speedBonusModifier = null;
        }
    }

    private void HandleSkillCanceled()
    {
        _player.Move.StopLookAt();
        Targeting.ClearTarget();
        Targeting.ClearTempTarget();
        if (coroutineDurationChanceApplyBleedingWithJump != null) StopCoroutine(IDurationChanceApplyBleedingWithJump());
        AnimCastEnded();
    }

    public void TrySpendAttackingPsi()
    {
        _spentAttackingPsiEnergy = _attackingPsionicEnergy.CurrentValue;
        CmdUseAttackingEnergy(_attackingPsionicEnergy.CurrentValue);
    }

    public void DurationChanceApplyBleedingWithJump()
    {
        if (coroutineDurationChanceApplyBleedingWithJump != null) StopCoroutine(IDurationChanceApplyBleedingWithJump());
        coroutineDurationChanceApplyBleedingWithJump = StartCoroutine(IDurationChanceApplyBleedingWithJump());
    }

    private IEnumerator IDurationChanceApplyBleedingWithJump()
    {
        _isDurationChanceApplyBleedingWithJump = true;
        yield return _waitForBuffDuration;
        _isDurationChanceApplyBleedingWithJump = false;
    }

    private bool CheckStateForBleeding(Character target)
    {
        States[] blockingStates = { States.Stun, States.Stupefaction, States.TentacleGrip };
        if (blockingStates.Any(state => target.CharacterState.CheckForState(state))) return true;
        else return false;
    }

    [Command]
    private void CmdAddBleeding(Character target)
    {
        target.CharacterState.AddState(States.BleedingCarry, _durationBleeding, 0.003f, _player.gameObject, "ClawStrike");
    }

    [Command]
    private void CmdUseAttackingEnergy(float value)
    {
        _attackingPsionicEnergy.CurrentValue -= value;
    }


    [Command]
    private void CmdDispel(Character targetCharacter, int dispelCount)
    {
        targetCharacter.CharacterState.DispelStates(StateType.Magic, targetCharacter.NetworkSettings.TeamIndex, _player.NetworkSettings.TeamIndex, dispelCount);
    }
    protected override void ClearData()
    {
        RemoveSpeedModifier();
        Targeting.ClearTarget();
        Targeting.ClearTempTarget();
        if (coroutineDurationChanceApplyBleedingWithJump != null) StopCoroutine(IDurationChanceApplyBleedingWithJump());
        AnimCastEnded();
    }
}