using Mirror;
using System.Collections;
using UnityEngine;

public class DoubleCheliceraStrike : Skill
{
    [SerializeField] private CheliceraStrike cheliceraStrike;
    [SerializeField] private CooldownEnergy cooldownEnergy;
    [SerializeField] private float _cheliceraStrikeBaseDamage;
    [SerializeField] private float _damageMultiplier = 0.75f * 2f;
    [SerializeField] private float _stunDuration = 1f;
    [SerializeField] private float _stunDurationWithJumpBack = 2f;
    [SerializeField] private float cooldownEnergyCost = 5;

    private Character _target;
    private Character _runtimeTarget;

    private static readonly int DoubleCheliceraStrikeAnimTrigger = Animator.StringToHash("DoubleCheliceraStrikeAnimation");

    protected override int AnimTriggerCast => DoubleCheliceraStrikeAnimTrigger;
    protected override int AnimTriggerCastDelay => 0;

    protected override bool IsCanCast => IsTargetInRange() &&  _target != null && cooldownEnergy.CurrentValue >= cooldownEnergyCost;

    private void OnEnable()
    {
        _cheliceraStrikeBaseDamage = cheliceraStrike.Damage;
        OnSkillCanceled += HandleSkillCanceled;
    }

    private void OnDestroy() => OnSkillCanceled -= HandleSkillCanceled;

    protected override IEnumerator PrepareJob(System.Action<TargetInfo> callbackDataSaved)
    {
        while (_target == null)
        {
            if (GetMouseButton)
            {
                _target = GetRaycastTarget();

                if (_target != null)
                {
                    _runtimeTarget = _target;
                    _target.SelectedCircle.IsActive = true;
                    _isCanCancle = false;
                }

                break;
            }

            yield return null;
        }

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Targets.Add(_runtimeTarget);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (_target == null) yield return null;

        DealDoubleCheliceraStrikeDamage(_target);

        cooldownEnergy.CastCooldownEnergySkill(cooldownEnergyCost, this);
        _hero.Move.StopLookAt();
        _hero.Move.CanMove = true;

        yield return null;
    }

    private bool IsTargetInRange()
    {
        return _target != null && Vector3.Distance(_hero.transform.position, _target.transform.position) <= Radius;
    }

    private void HandleSkillCanceled()
    {
        _target = null;
        Hero.Move.StopLookAt();
        _hero.Move.CanMove = true;
    }

    private void DealDoubleCheliceraStrikeDamage(Character targetCharacter)
    {
        float totalDamage = _cheliceraStrikeBaseDamage * _damageMultiplier;

        Damage damage = new Damage
        {
            Value = totalDamage,
            Type = DamageType.Physical,
            PhysicAttackType = AttackRangeType.MeleeAttack
        };

        CmdApplyDamage(damage, targetCharacter.gameObject);
        CmdApplyStun(targetCharacter);
    }

    public void DoubleCheliceraStrikeAnimationMove()
    {
        if (_hero == null || _hero.Move == null) return;

        if (_target == null)
        {
            _hero.Move.StopLookAt();
            return;
        }

        _hero.Move.StopMoveAndAnimationMove();
        _hero.Move.CanMove = false;

        Vector3 direction = _target.transform.position - _hero.transform.position;
        bool badDirection = float.IsInfinity(_target.transform.position.x) || direction.sqrMagnitude < 0.0001f;

        if (badDirection)
        {
            _hero.Move.StopLookAt();
            return;
        }

        _hero.Move.LookAtPosition(_target.transform.position);
    }

    public void DoubleCheliceraStrikeCast()
    {
        Hero.Animator.speed = Hero.Animator.speed / 1.6f;
        AnimStartCastCoroutine();
    }

    public void DoubleCheliceraStrikeEnded()
    {
        Hero.Animator.speed = 1;
        _isCanCancle = true;
        AnimCastEnded();
    }

    [Command]
    private void CmdApplyStun(Character target)
    {
        var lastSkill = Hero.Abilities.LastCastedSkill;

        if ((lastSkill is JumpBack))  target.CharacterState.AddState(States.Stun, _stunDurationWithJumpBack, 0, _hero.gameObject, null);
        else target.CharacterState.AddState(States.Stun, _stunDuration, 0, _hero.gameObject, null);
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.Targets.Count > 0) _target = (Character)targetInfo.Targets[0];
        _hero.Move.LookAtTransform(_target.transform);
    }

    protected override void ClearData()
    {
        _target = null;
    }
}