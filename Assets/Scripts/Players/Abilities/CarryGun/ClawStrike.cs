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

    private bool _isDurationChanceApplyBleedingWithJump = false;
    private float _spentAttackingPsiEnergy;
    private float _baseDamage;
    private float _castWindowDuration = 1f;
    private float _totalChanceApplyBleeding;
    private Coroutine coroutineDurationChanceApplyBleedingWithJump;

    protected IDamageable _target;
    private Character _runtimeTarget;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("ClawStrikeTrigger");

    protected override bool IsCanCast => _target != null && Vector3.Distance(_target.transform.position, transform.position) <= Radius && NoObstacles(_target.transform.position, transform.position, _obstacle);

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

    public void ClawStrikeSpeed(bool value) => Hero.Animator.speed = value ? 1.4f : 1f;
    public void BleedingClawStrike(bool value) => _isBleedingClawStrike = value;
    public void ChanceApplyBleedingIncrease(bool value) => _isChanceApplyBleedingIncrease = value;
    #endregion

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        _runtimeTarget = null;

        while (_target == null)
        {
            if (GetMouseButton)
            {
                _target = GetRaycastTarget();

                if (_target != null)
                {
                    if (_target is Character characterTarget)
                    {
                        characterTarget.SelectedCircle.IsActive = true;
                        _runtimeTarget = characterTarget;
                    }
                }

                _isCanCancle = false;
            }
            yield return null;
        }

        TargetInfo targetInfo = new TargetInfo();
        if (_runtimeTarget is Character character) targetInfo.Targets.Add(character);
        callbackDataSaved?.Invoke(targetInfo);
    }


    protected override IEnumerator CastJob()
    {
        if (_target == null) yield return null;
        if (!IsTargetInRange()) yield return null;

        JumpBackClawStrike();
        DamageDeal();

        yield return null;
    }

    private bool IsTargetInRange() { return _target != null && Vector3.Distance(_player.transform.position, _target.transform.position) <= Radius; }

    private void DamageDeal()
    {
        if (_target == null) return;

        float attackingPsiValue = _spentAttackingPsiEnergy;
        _baseDamage = UnityEngine.Random.Range(5f, 7.01f);

        var damage = new Damage
        {
            Value = _baseDamage,
            Type = DamageType.Physical,
            PhysicAttackType = AttackRangeType.MeleeAttack,
        };

        CmdApplyDamage(damage, _target.gameObject);

        TryApplyBleeding();

        if (attackingPsiValue > 0)
        {
            var additionalDamage = attackingPsiValue;

            int dispelCount = 0;

            if (attackingPsiValue >= 30) dispelCount = 3;
            else if (attackingPsiValue >= 20) dispelCount = 2;
            else if (attackingPsiValue >= 10) dispelCount = 1;

            if (dispelCount > 0) for (int i = 0; i < dispelCount; i++) CmdDispel(_runtimeTarget, dispelCount);

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

    private void TryApplyBleeding()
    {
        if (!_isBleedingClawStrike) return;

        _totalChanceApplyBleeding = chanceApplyBleeding;
        var lastSkill = _player.Abilities.LastCastedSkill;

        if (_isDurationChanceApplyBleedingWithJump && jumpWithChelicera.IsCheliceraStrikeCast && lastSkill is CheliceraStrike) _totalChanceApplyBleeding = chanceApplyBleedingWithJump;

        if (_isChanceApplyBleedingIncrease && CheckStateForBleeding()) _totalChanceApplyBleeding += chanceApplyBleedingIncrease;

        float rand = UnityEngine.Random.Range(0f, 1f);
        if (rand <= _totalChanceApplyBleeding) CmdAddBleeding(_runtimeTarget);

        jumpWithChelicera.IsCheliceraStrikeCast = false;
        _isDurationChanceApplyBleedingWithJump = false;
        if (coroutineDurationChanceApplyBleedingWithJump != null) StopCoroutine(IDurationChanceApplyBleedingWithJump());
    }

    public void ClawStrikeSpeedAnim()
    {
        _player.Animator.SetFloat("ClawStrikeSpeed", 1f / animSpeed);
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
        _isCanCancle = true;
    }

    private void HandleSkillCanceled()
    {
        _isCanCancle = true;
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
    
    private bool CheckStateForBleeding()
    {
        States[] blockingStates = { States.Stun, States.Stupefaction, States.TentacleGrip };
        if (blockingStates.Any(state => _runtimeTarget.CharacterState.CheckForState(state))) return true;
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

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.Targets.Count > 0) _target = targetInfo.Targets[0] as Character;
        _isCanCancle = false;
    }

    protected override void ClearData()
    {
        _target = null;
    }
}
