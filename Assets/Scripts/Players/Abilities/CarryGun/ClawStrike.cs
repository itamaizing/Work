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
    [SerializeField] private JumpWithChelicera jumpWithChelicera;
    [SerializeField] private JumpBack jumpBack;
    [SerializeField] private float animSpeed = 0.8f;
    [SerializeField] private float chanceApplyBleeding = 0.15f;
    [SerializeField] private float chanceApplyBleedingWithJump = 0.4f;
    [SerializeField] private float durationBleeding = 7f;
    [SerializeField] private float buffDurationAfterJump = 1f;
    [SerializeField] private float chanceApplyBleedingIncrease = 0.4f;

    [Header("Damage")]
    [SerializeField] private float minDamage = 10f;
    [SerializeField] private float maxDamage = 11f;

    private bool _isDurationChanceApplyBleedingWithJump = false;
    private bool _isAnimationAcceleration = false;
    private bool _isLastClawStrike;
    private float _spentAttackingPsiEnergy;
    private float _baseDamage;
    private float _castWindowDuration = 1f;
    private float _totalChanceApplyBleeding;
    private Coroutine coroutineDurationChanceApplyBleedingWithJump;

    protected IDamageable _target;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("ClawStrikeTrigger");
    protected override bool IsCanCast
    {
        get
        {
            if (_target == null) return false;

            var targetGO = _target.gameObject;
            if (targetGO == null) return false;

            var targetTransform = _target.transform;
            if (targetTransform == null) return false;

            return Vector3.Distance(targetTransform.position, transform.position) <= Radius &&
                   NoObstacles(targetTransform.position, transform.position, _obstacle);
        }
    }

    public float CastWindowDuration { get => _castWindowDuration; set => _castWindowDuration = value; }

    private void OnDisable()
    {
        OnSkillCanceled -= HandleSkillCanceled;
    }

    private void OnEnable()
    {
        OnSkillCanceled += HandleSkillCanceled;
    }

    #region Talent
    private bool _isBleedingClawStrike  = false;
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
        if (targetInfo.Targets.Count > 0) _target = targetInfo.Targets[0] as IDamageable;
        if (_target is Character character && character.SelectedCircle != null) character.SelectedCircle.IsActive = false;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
         ITargetable target = null;

        while (target == null)
        {
            if (GetMouseButton)
            {
                if (GetRaycastTarget() is ITargetable targetable) target = targetable;
            }
            yield return null;
        }

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Targets.Add(target);
        callbackDataSaved.Invoke(targetInfo);
    }


    protected override IEnumerator CastJob()
    {
        if (_target == null) yield return null;
        if (!IsTargetInRange()) yield return null;

        JumpBackClawStrike();
        DamageDeal(_target);

        yield return null;
    }

    private bool IsTargetInRange() { return _target != null && Vector3.Distance(_player.transform.position, _target.transform.position) <= Radius; }

    private void DamageDeal(IDamageable target)
    {
        if (target == null) return;

        float attackingPsiValue = _spentAttackingPsiEnergy;
        _baseDamage = UnityEngine.Random.Range(minDamage, maxDamage);
        Damage = _baseDamage;

        var damage = new Damage
        {
            Value = _baseDamage,
            Type = DamageType.Physical,
            PhysicAttackType = AttackRangeType.MeleeAttack,
        };

        CmdApplyDamage(damage, _target.gameObject);

        Character targetCharacter = target as Character;

        if (targetCharacter != null) TryApplyBleeding(targetCharacter);

        if (attackingPsiValue > 0)
        {
            var additionalDamage = attackingPsiValue;

            int dispelCount = 0;

            if (attackingPsiValue >= 30) dispelCount = 3;
            else if (attackingPsiValue >= 20) dispelCount = 2;
            else if (attackingPsiValue >= 10) dispelCount = 1;

            if (dispelCount > 0 && targetCharacter != null) for (int i = 0; i < dispelCount; i++) CmdDispel(targetCharacter, dispelCount);

            var damagePsi = new Damage
            {
                Value = additionalDamage,
                Type = DamageType.Magical,
                PhysicAttackType = AttackRangeType.MeleeAttack,
            };

            CmdApplyDamage(damagePsi, _target.gameObject);
        }

    }

    private void JumpBackClawStrike()
    {
        var lastSkill = _player.Abilities.LastCastedSkill;
        if (jumpBack != null && (lastSkill is CheliceraStrike || lastSkill is ClawStrike)) jumpBack.EnableJumpBack();
    }

    private void TryApplyBleeding(Character target)
    {
        if (!_isBleedingClawStrike) return;

        _totalChanceApplyBleeding = chanceApplyBleeding;
        var lastSkill = _player.Abilities.LastCastedSkill;

        if (_isDurationChanceApplyBleedingWithJump && jumpWithChelicera.IsCheliceraStrikeCast && lastSkill is CheliceraStrike) _totalChanceApplyBleeding = chanceApplyBleedingWithJump;

        if (_isChanceApplyBleedingIncrease && CheckStateForBleeding(target)) _totalChanceApplyBleeding += chanceApplyBleedingIncrease;

        float rand = UnityEngine.Random.Range(0f, 1f);
        if (rand <= _totalChanceApplyBleeding) CmdAddBleeding(target);

        jumpWithChelicera.IsCheliceraStrikeCast = false;
        _isDurationChanceApplyBleedingWithJump = false;
        if (coroutineDurationChanceApplyBleedingWithJump != null) StopCoroutine(IDurationChanceApplyBleedingWithJump());
    }

    public void ClawStrikePreparingForAnim()
    {
        var lastSkill = _player.Abilities.LastCastedSkill;
        float multiplier = 0;

        if (_isAnimationAcceleration)
        {
            if ((lastSkill is ClawStrike && _isLastClawStrike) || lastSkill is CheliceraStrike)
            {
                multiplier = 1.4f;
                _isLastClawStrike = false;
            }

            else
            {
                multiplier = 1f;
                _isLastClawStrike = lastSkill is ClawStrike;
            }
        }

        else multiplier = 1f;

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
        _target = null;
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
        yield return new WaitForSeconds(buffDurationAfterJump);
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
        target.CharacterState.AddState(States.Bleeding, durationBleeding, 0, _player.gameObject, null);
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
        _target = null;
    }
}
