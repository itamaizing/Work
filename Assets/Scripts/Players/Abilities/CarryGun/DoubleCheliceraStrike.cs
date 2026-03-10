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

    private static readonly int DoubleCheliceraStrikeAnimTrigger = Animator.StringToHash("DoubleCheliceraStrikeAnimation");

    protected override int AnimTriggerCast => DoubleCheliceraStrikeAnimTrigger;
    protected override int AnimTriggerCastDelay => 0;

    protected override bool IsCanCast => IsTargetInRange() && cooldownEnergy.CurrentValue >= cooldownEnergyCost;

    private void OnEnable()
    {
        _cheliceraStrikeBaseDamage = cheliceraStrike.Damage;
        OnSkillCanceled += HandleSkillCanceled;
    }

    private void OnDisable() => OnSkillCanceled -= HandleSkillCanceled;

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        //_runtimeTarget = null;

        while (Targeting.GetTarget()?.Character == null)
        {
            if (GetMouseButton)
            {
                Targeting.FindTempTarget();
                //_target = GetRaycastTarget();

                if (Targeting.GetTarget()?.Character != null)
                {
                    if (Targeting.GetTarget()?.Character is Character characterTarget)
                    {
                        //_runtimeTarget = characterTarget;
                        characterTarget.SelectedCircle.IsActive = true;
                    }
                }
            }
            yield return null;
        }

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(Targeting.GetTarget()?.Character);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (Targeting.GetTarget()?.Character == null) yield return null;

        DealDoubleCheliceraStrikeDamage(Targeting.GetTarget()?.Character);

        cooldownEnergy.CastCooldownEnergySkill(cooldownEnergyCost, this);

        yield return null;
    }

    private bool IsTargetInRange()
    {
        return Targeting.GetTarget()?.Character != null &&
            Vector3.Distance(Targeting.GetTarget().Character.transform.position, transform.position) <= AreaInfo.Radius &&
            Targeting.NoObstacles(Targeting.GetTarget().Character.transform.position, transform.position, _obstacle);
    }

    private void HandleSkillCanceled()
    {
        Targeting.ClearTarget();
        //_target = null;
        _isCanCancel = true;
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
        _isCanCancel = true;
        AnimCastEnded();
    }

    [Command]
    private void CmdApplyStun(Character target)
    {
        var lastSkill = _player.Abilities.LastCastedSkill;

        if ((lastSkill is JumpBack))  target.CharacterState.AddState(States.Stun, _stunDurationWithJumpBack, 0, _player.gameObject, null);
        else target.CharacterState.AddState(States.Stun, _stunDuration, 0, _player.gameObject, null);
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0) Targeting.SetTarget((ITargetable)(Character)targetInfo.GetTargets()[0]);
        _isCanCancel = false;
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();
        //_target = null;
    }
}

