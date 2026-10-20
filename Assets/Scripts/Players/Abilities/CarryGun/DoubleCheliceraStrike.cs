using Mirror;
using System.Collections;
using SkillSystem.States;
using UnityEngine;

public class DoubleCheliceraStrike : Skill
{
    [SerializeField] private Character _player;
    [SerializeField] private CheliceraStrike cheliceraStrike;
    [SerializeField] private JumpBack _jumpBack;
    [SerializeField] private CooldownEnergy cooldownEnergy;

    [Header("Damage")]
    [SerializeField] private float _damageMultiplierPerHit = 0.75f;
    [SerializeField] private float _secondHitDelay = 0.2f;

    [Header("Stun")]
    [SerializeField] private float _stunDuration = 1f;
    [SerializeField] private float _stunDurationWithJumpBack = 2f;
    [SerializeField] private float _jumpBackStunWindow = 2f;

    [SerializeField] private float cooldownEnergyCost = 5;

    private static readonly int DoubleCheliceraStrikeAnimTrigger = Animator.StringToHash("DoubleCheliceraStrikeAnimation");

    protected override int AnimTriggerCast => DoubleCheliceraStrikeAnimTrigger;
    protected override int AnimTriggerPrepare => 0;

    protected override bool IsCanCast => IsTargetInRange() && cooldownEnergy.CurrentValue >= cooldownEnergyCost;

    private void OnEnable() => OnSkillCanceled += HandleSkillCanceled;
    private void OnDisable() => OnSkillCanceled -= HandleSkillCanceled;

    protected override IEnumerator CastJob()
    {
        var target = Targeting.GetTarget()?.Character;
        if (target == null) yield break;

        bool afterJumpBack = _hero.Abilities.LastCastedSkill is JumpBack jump
                             && jump.LastEndReason == SkillEndReason.Completed;
        
        cheliceraStrike.RollStrike(target, _damageMultiplierPerHit);
        cooldownEnergy.CastCooldownEnergySkill(cooldownEnergyCost, this);

        if (_secondHitDelay > 0f) yield return new WaitForSeconds(_secondHitDelay);

        if (target != null && !target.IsDead)
        {
            cheliceraStrike.RollStrike(target, _damageMultiplierPerHit);
            CmdApplyStun(target, afterJumpBack);
        }

        ComboContext.ClawStrikeContext.Set(typeof(DoubleCheliceraStrike));
    }

    private bool IsTargetInRange()
    {
        return Targeting.GetTarget()?.Character != null &&
            Vector3.Distance(Targeting.GetTarget().Character.transform.position, transform.position) <= AreaInfo.Radius &&
            Targeting.NoObstacles(Targeting.GetTarget().Character.transform.position, transform.position, Targeting.AdditionalObstacle);
    }

    private void HandleSkillCanceled()
    {
        Targeting.ClearTarget();
        _isCanCancel = true;
    }

    public void DoubleCheliceraStrikeCast() => AnimStartCastCoroutine();

    public void DoubleCheliceraStrikeEnded()
    {
        _isCanCancel = true;
        AnimCastEnded();
    }

    [Command]
    private void CmdApplyStun(Character target, bool afterJumpBack)
    {
        if (target == null) return;
        float duration = afterJumpBack ? _stunDurationWithJumpBack : _stunDuration;
        target.CharacterState.AddState(States.Stun, duration, 0, _player.gameObject, null);
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0) Targeting.SetTarget((ITargetable)(Character)targetInfo.GetTargets()[0]);
        _isCanCancel = false;
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();
    }
}