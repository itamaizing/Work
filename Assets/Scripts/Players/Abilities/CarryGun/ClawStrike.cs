using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class ClawStrike : Skill
{
    [SerializeField] private Character _player;
    [SerializeField] private BasePsionicEnergy _basePsionicEnergy;
    [SerializeField] private AttackingPsionicEnergy _attackingPsionicEnergy;
    [SerializeField] private JumpWithChelicera jumpWithChelicera;
    [SerializeField] private float animSpeed = 0.8f;
    [SerializeField] private float chanceApplyBleeding = 0.15f;
    [SerializeField] private float chanceApplyBleedingWithJump = 0.4f;
    [SerializeField] private float durationBleeding = 7f;
    [SerializeField] private float buffDurationAfterJump = 1f;

    private bool _isDurationChanceApplyBleedingWithJump = false;
    private float _spentAttackingPsiEnergy;
    private float _baseDamage;
    private Coroutine coroutineDurationChanceApplyBleedingWithJump;

    protected Character _target;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("ClawStrikeTrigger");

    protected override bool IsCanCast => _target != null && Vector3.Distance(_target.transform.position, transform.position) <= Radius && NoObstacles(_target.transform.position, transform.position, _obstacle);

    private void OnDisable() => OnSkillCanceled -= HandleSkillCanceled;
    private void OnEnable() => OnSkillCanceled += HandleSkillCanceled;

    #region Talent
    private bool _isBleedingClawStrike  = false;

    public void ClawStrikeSpeed(bool value) => Hero.Animator.speed = value ? 1.4f : 1f;
    public void BleedingClawStrike(bool value) => _isBleedingClawStrike = value;
    #endregion

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();

        if (_target != null)
        {
            _hero.Move.LookAtTransform(_target.transform);
            targetInfo.Targets.Add(_target);
            targetInfo.Points.Add(_target.transform.position);
            callbackDataSaved?.Invoke(targetInfo);
            yield break;
        }

        while (_target == null)
        {
            if (GetMouseButton)
            {
                _target = GetRaycastTarget();
                if (_target != null)
                {
                    _target.SelectedCircle.IsActive = true;
                    _hero.Move.LookAtTransform(_target.transform);
                    break;
                }
            }
            yield return null;
        }

        targetInfo.Targets.Add(_target);
        targetInfo.Points.Add(_target.transform.position);
        callbackDataSaved?.Invoke(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (_target == null) yield return null;
        if (!IsTargetInRange()) yield return null;

        DamageDeal();

        _target = null;
        _hero.Move.StopLookAt();
        yield return null;
    }

    private bool IsTargetInRange() { return _target != null && Vector3.Distance(_player.transform.position, _target.transform.position) <= Radius; }

    private void DamageDeal()
    {
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

            if (dispelCount > 0) for (int i = 0; i < dispelCount; i++) CmdDispel(_target, dispelCount);

            var damagePsi = new Damage
            {
                Value = additionalDamage,
                Type = DamageType.Magical,
                PhysicAttackType = AttackRangeType.MeleeAttack,
            };

            CmdApplyDamage(damagePsi, _target.gameObject);
        }

    }

    private void TryApplyBleeding()
    {
        if (!_isBleedingClawStrike) return;


        float chance = chanceApplyBleeding;
        if (_isDurationChanceApplyBleedingWithJump) chance = chanceApplyBleedingWithJump;


        float rand = UnityEngine.Random.Range(0f, 1f);
        if (rand <= chance) CmdAddBleeding(_target);

        if (coroutineDurationChanceApplyBleedingWithJump != null) StopCoroutine(IDurationChanceApplyBleedingWithJump());
        _isDurationChanceApplyBleedingWithJump = false;
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
    }

    private void HandleSkillCanceled()
    {
        _target = null;
        Hero.Move.StopLookAt();
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
        yield return new WaitForSeconds(buffDurationAfterJump);
        _isDurationChanceApplyBleedingWithJump = true;
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
        if (targetInfo == null || targetInfo.Targets == null || targetInfo.Targets.Count == 0) return;

        _target = targetInfo.Targets[0] as Character;
    }

    protected override void ClearData()
    {

    }
}
