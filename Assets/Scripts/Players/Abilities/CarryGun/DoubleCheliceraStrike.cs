using Mirror;
using System.Collections;
using UnityEngine;

public class DoubleCheliceraStrike : Skill
{
    [SerializeField] private Character _target;
    [SerializeField] private CheliceraStrike cheliceraStrike;
    [SerializeField] private float _cheliceraStrikeBaseDamage;
    [SerializeField] private float _damageMultiplier = 0.75f * 2f;
    [SerializeField] private float _stunDuration = 1f;
    [SerializeField] private float _stunDurationWithJumpBack = 2f;

    private static readonly int DoubleCheliceraStrikeAnimTrigger = Animator.StringToHash("DoubleCheliceraStrikeAnimation");

    protected override int AnimTriggerCast => DoubleCheliceraStrikeAnimTrigger;
    protected override int AnimTriggerCastDelay => 0;

    protected override bool IsCanCast => IsTargetInRange() &&  _target != null;

    private void OnEnable() => _cheliceraStrikeBaseDamage = cheliceraStrike.Damage;

    protected override IEnumerator PrepareJob(System.Action<TargetInfo> callbackDataSaved)
    {
        while (_target == null)
        {
            if (GetMouseButton)
            {
                _target = GetRaycastTarget();

                if (_target != null)
                    _target.SelectedCircle.IsActive = true;
            }

            yield return null;
        }

        _hero.Move.LookAtTransform(_target.transform);

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Points.Add(_target.transform.position);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        DealDoubleCheliceraStrikeDamage(_target);

        _target = null;
        _hero.Move.StopLookAt();
        _hero.Move.CanMove = true;

        yield return null;
    }

    private bool IsTargetInRange()
    {
        return Vector3.Distance(_hero.transform.position, _target.transform.position) <= Radius;
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
        AnimStartCastCoroutine();
    }

    public void DoubleCheliceraStrikeEnded()
    {
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
    }

    protected override void ClearData()
    {
        _target = null;
    }
}