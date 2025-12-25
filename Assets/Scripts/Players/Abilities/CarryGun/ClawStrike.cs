using Mirror;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public class ClawStrike : Skill
{
    [SerializeField] private Character _player;
    [SerializeField] private BasePsionicEnergy _basePsionicEnergy;
    [SerializeField] private AttackingPsionicEnergy _attackingPsionicEnergy;
    [SerializeField] private JumpWithChelicera _jumpWithChelicera;
    [SerializeField] private JumpBack _jumpBack;
    [SerializeField] private float _animSpeed = 0.8f;
    [SerializeField] private float _chanceApplyBleeding = 0.15f;
    [SerializeField] private float _chanceApplyBleedingWithJump = 0.4f;
    [SerializeField] private float _durationBleeding = 7f;
    [SerializeField] private float _buffDurationAfterJump = 1f;
    [SerializeField] private float _chanceApplyBleedingIncrease = 0.4f;

    [Header("Damage")]
    [SerializeField] private float _minDamage = 10f;
    [SerializeField] private float _maxDamage = 11f;

    #region Constants

    private const float AnimationSpeedDefault = 1f;
    private const float AnimationSpeedFast = 1.4f;

    private const float RandomChanceMin = 0f;
    private const float RandomChanceMax = 1f;

    private const float PsiDispel_3 = 30f;
    private const float PsiDispel_2 = 20f;
    private const float PsiDispel_1 = 10f;

    private const float TargetSearchRadius = 0.5f;

    #endregion

    private bool _isDurationChanceApplyBleedingWithJump = false;
    private bool _isAnimationAcceleration = false;
    private bool _isLastClawStrike;
    private float _spentAttackingPsiEnergy;
    private float _baseDamage;
    private float _castWindowDuration = 1f;
    private float _totalChanceApplyBleeding;
    private Coroutine coroutineDurationChanceApplyBleedingWithJump;
    private WaitForSeconds _waitForBuffDuration;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("ClawStrikeTrigger");
    protected override bool IsCanCast => CheckIsCanCast();
    private bool IsAllyTarget(IDamageable target) => target.gameObject.layer == LayerMask.NameToLayer("Allies");

    public float CastWindowDuration { get => _castWindowDuration; set => _castWindowDuration = value; }

    private bool CheckIsCanCast()
    {
        return GetTarget() != null &&
            Vector3.Distance(GetTarget().Transform.position, transform.position) <= Radius &&
            NoObstacles(GetTarget().Transform.position, transform.position, _obstacle);
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

    public void ClawStrikeSpeed(bool value)
    {
        _isAnimationAcceleration = value;
    }

    public void BleedingClawStrike(bool value) => _isBleedingClawStrike = value;
    public void ChanceApplyBleedingIncrease(bool value) => _isChanceApplyBleedingIncrease = value;
    #endregion
    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0) SetTarget(targetInfo.GetTargets()[0]);
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();

        while (GetTempTarget() == null)
        {
            if (GetMouseButton)
            {
                FindTarget(TargetSearchRadius, GetMousePoint());

                if (GetTempTarget() != null && GetTempTarget() is IDamageable damageable)
                {
                    if (IsAllyTarget(damageable) || damageable as Character == Hero) ClearTempTarget();

                    else
                    {
                        if (GetTempTarget() is Character character && character.SelectedCircle != null) character.SelectedCircle.IsActive = false;
                        break;
                    }
                }
            }
            yield return null;
        }

        SetTarget(GetTempTarget());

        targetInfo.Points.Add(GetTarget().Transform.position);
        targetInfo.AddTarget(GetTarget());
        callbackDataSaved.Invoke(targetInfo);
    }


    protected override IEnumerator CastJob()
    {
        if (GetTarget() == null) yield return null;
        if (!IsTargetInRange()) yield return null;

        JumpBackClawStrike();
        DamageDeal(GetTarget());

        yield return null;
    }

    private bool IsTargetInRange() { return GetTarget() != null && Vector3.Distance(_player.transform.position, GetTarget().Transform.position) <= Radius; }

    private void DamageDeal(ITargetable target)
    {
        if (target == null) return;

        IDamageable damageable = target as IDamageable;
        float attackingPsiValue = _spentAttackingPsiEnergy;
        _baseDamage = UnityEngine.Random.Range(_minDamage, _maxDamage);
        Damage = _baseDamage;

        var damage = new Damage
        {
            Value = _baseDamage,
            Type = DamageType.Physical,
            PhysicAttackType = AttackRangeType.MeleeAttack,
        };

        CmdApplyDamage(damage, damageable.gameObject);

        Character targetCharacter = target as Character;

        if (targetCharacter != null) TryApplyBleeding(targetCharacter);

        if (attackingPsiValue > 0)
        {
            var additionalDamage = attackingPsiValue;

            int dispelCount = 0;

            if (attackingPsiValue >= PsiDispel_3) dispelCount = 3;
            else if (attackingPsiValue >= PsiDispel_2) dispelCount = 2;
            else if (attackingPsiValue >= PsiDispel_1) dispelCount = 1;

            if (dispelCount > 0 && targetCharacter != null) for (int i = 0; i < dispelCount; i++) CmdDispel(targetCharacter, dispelCount);

            var damagePsi = new Damage
            {
                Value = additionalDamage,
                Type = DamageType.Magical,
                PhysicAttackType = AttackRangeType.MeleeAttack,
            };

            CmdApplyDamage(damagePsi, damageable.gameObject);
        }

    }

    private void JumpBackClawStrike()
    {
        var lastSkill = _player.Abilities.LastCastedSkill;
        if (_jumpBack != null && (lastSkill is CheliceraStrike || lastSkill is ClawStrike)) _jumpBack.EnableJumpBack();
    }

    private void TryApplyBleeding(Character target)
    {
        if (!_isBleedingClawStrike) return;

        _totalChanceApplyBleeding = _chanceApplyBleeding;
        var lastSkill = _player.Abilities.LastCastedSkill;

        if (_isDurationChanceApplyBleedingWithJump && _jumpWithChelicera.IsCheliceraStrikeCast && lastSkill is CheliceraStrike) _totalChanceApplyBleeding = _chanceApplyBleedingWithJump;

        if (_isChanceApplyBleedingIncrease && CheckStateForBleeding(target)) _totalChanceApplyBleeding += _chanceApplyBleedingIncrease;

        float rand = UnityEngine.Random.Range(RandomChanceMin, RandomChanceMax);
        if (rand <= _totalChanceApplyBleeding) CmdAddBleeding(target);

        _jumpWithChelicera.IsCheliceraStrikeCast = false;
        _isDurationChanceApplyBleedingWithJump = false;
        if (coroutineDurationChanceApplyBleedingWithJump != null) StopCoroutine(IDurationChanceApplyBleedingWithJump());
    }

    public void ClawStrikePreparingForAnim()
    {
        var lastSkill = _player.Abilities.LastCastedSkill;
        float multiplier;

        if (_isAnimationAcceleration)
        {
            if ((lastSkill is ClawStrike && _isLastClawStrike) || lastSkill is CheliceraStrike)
            {
                multiplier = AnimationSpeedFast;
                _isLastClawStrike = false;
            }

            else
            {
                multiplier = AnimationSpeedDefault;
                _isLastClawStrike = lastSkill is ClawStrike;
            }
        }

        else multiplier = AnimationSpeedDefault;

        Hero.Animator.SetFloat("ClawStrikeSpeed", multiplier);

        if (_attackingPsionicEnergy.IsAttackingPsiEnergy && _attackingPsionicEnergy.CurrentValue > 0f) TrySpendAttackingPsi();
        else _spentAttackingPsiEnergy = 0;
    }

    public void ClawStrikeCast()
    {
        AnimStartCastCoroutine();
    }

    public void ClawStrikeEnded()
    {
        AnimCastEnded();
    }

    private void HandleSkillCanceled()
    {
        _player.Move.StopLookAt();
        ClearTarget();
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
        target.CharacterState.AddState(States.Bleeding, _durationBleeding, 0, _player.gameObject, null);
    }

    [Command]
    private void CmdUseAttackingEnergy(float value)
    {
        _attackingPsionicEnergy.CurrentValue -= value;
    }


    [Command]
    private void CmdDispel(Character targetCharacter, float dispelCount)
    {
        targetCharacter.CharacterState.DispelStates(StateType.Magic, targetCharacter.NetworkSettings.TeamIndex, _player.NetworkSettings.TeamIndex, dispelCount > 0);
    }
    protected override void ClearData()
    {
        ClearTarget();
        if (coroutineDurationChanceApplyBleedingWithJump != null) StopCoroutine(IDurationChanceApplyBleedingWithJump());
        AnimCastEnded();
    }
}