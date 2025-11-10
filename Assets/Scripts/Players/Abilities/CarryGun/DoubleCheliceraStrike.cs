using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class DoubleCheliceraStrike : Skill
{
    [SerializeField] private Character _player;
    [SerializeField] private CheliceraStrike cheliceraStrike;
    [SerializeField] private CooldownEnergy cooldownEnergy;
    [SerializeField] private float _cheliceraStrikeBaseDamage;
    [SerializeField] private float _damageMultiplier = 0.75f * 2f;
    [SerializeField] private float _stunDuration = 1f;
    [SerializeField] private float _stunDurationWithJumpBack = 2f;
    [SerializeField] private float cooldownEnergyCost = 5;

    private IDamageable _target;

    private static readonly int DoubleCheliceraStrikeAnimTrigger = Animator.StringToHash("DoubleCheliceraStrikeAnimation");

    protected override int AnimTriggerCast => DoubleCheliceraStrikeAnimTrigger;
    protected override int AnimTriggerCastDelay => 0;

    protected override bool IsCanCast => IsTargetInRange() && cooldownEnergy.CurrentValue >= cooldownEnergyCost;

    private void OnEnable()
    {
        _cheliceraStrikeBaseDamage = cheliceraStrike.Damage;
        OnSkillCanceled += HandleSkillCanceled;
    }

    private void OnDestroy() => OnSkillCanceled -= HandleSkillCanceled;

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.Targets.Count > 0) _target = (Character)targetInfo.Targets[0];
        if (_target is Character character) character.SelectedCircle.IsActive = true;
        _player.Move.CanMove = false;
        _isCanCancle = false;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        IDamageable target = null;

        while (target == null)
        {
            if (GetMouseButton)
            {
                if (GetRaycastTarget() is IDamageable iDamageable)
                {
                    target = iDamageable;
                }
            }
            yield return null;
        }

        TargetInfo targetInfo = new TargetInfo();
        if (target is Character character) targetInfo.Targets.Add(character);
        callbackDataSaved?.Invoke(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (_target == null) yield return null;

        DealDoubleCheliceraStrikeDamage(_target);

        cooldownEnergy.CastCooldownEnergySkill(cooldownEnergyCost, this);

        yield return null;
    }

    private bool IsTargetInRange()
    {
        return _target != null &&
            Vector3.Distance(_target.transform.position, transform.position) <= Radius &&
            NoObstacles(_target.transform.position, transform.position, _obstacle);
    }

    private void HandleSkillCanceled()
    {
        _target = null;
        _isCanCancle = true;
    }

    private void DealDoubleCheliceraStrikeDamage(IDamageable targetCharacter)
    {
        float totalDamage = _cheliceraStrikeBaseDamage * _damageMultiplier;

        Damage damage = new Damage
        {
            Value = totalDamage,
            Type = DamageType.Physical,
            PhysicAttackType = AttackRangeType.MeleeAttack
        };

        CmdApplyDamage(damage, targetCharacter.gameObject);
        if (targetCharacter is Character character) CmdApplyStun(character);
    }

    public void DoubleCheliceraStrikeCast()
    {
        AnimStartCastCoroutine();
    }

    public void DoubleCheliceraStrikeEnded()
    {
        _isCanCancle = true;
        AnimCastEnded();
    }

    [Command]
    private void CmdApplyStun(Character target)
    {
        var lastSkill = _player.Abilities.LastCastedSkill;

        if ((lastSkill is JumpBack))  target.CharacterState.AddState(States.Stun, _stunDurationWithJumpBack, 0, _player.gameObject, null);
        else target.CharacterState.AddState(States.Stun, _stunDuration, 0, _player.gameObject, null);
    }

    protected override void ClearData()
    {
        _target = null;
    }
}