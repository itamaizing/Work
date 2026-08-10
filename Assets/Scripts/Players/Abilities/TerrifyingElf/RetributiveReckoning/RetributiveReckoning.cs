using Mirror;
using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class RetributiveReckoning : Skill
{
    [Header("Retributive Reckoning Settings")]
    [SerializeField] private Health health;
    [SerializeField] private MoveComponent moveComponent;
    [SerializeField] private float disactiveResetTime = 1f;

    private Character _lastAttacker;
    private Coroutine _disactiveResetCoroutine;
    private Coroutine _magicBoostCoroutine;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    protected override bool IsCanCast => _lastAttacker != null && !Disactive;

    #region Talent
    private bool _isMagicAbilityInstantly;

    public void MagicAbilityInstantly(bool value) => _isMagicAbilityInstantly = value;
    #endregion

    private void Start()
    {
        Disactive = true;
        if (health != null)
        {
            health.DamageTaken += OnDamageTaken;
        }
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.DamageTaken -= OnDamageTaken;
        }
    }
    
    private void OnDamageTaken(Damage damage, Skill skill)
    {
        if (damage.Type != DamageType.Physical) return;
        if (skill == null || skill.Hero == null) return;

        Character attacker = skill.Hero as Character;
        if (attacker == null || !IsMeleeBackAttack(attacker)) return;

        CmdOnDamageTaken(attacker);
    }

    private bool IsMeleeBackAttack(Character attacker)
    {
        float distance = Vector3.Distance(attacker.transform.position, transform.position);
        if (distance > AreaInfo.Radius) return false;

        Vector3 directionToAttacker = (attacker.transform.position - transform.position).normalized;
        Vector3 forwardDirection = moveComponent.transform.forward.normalized;
        float angle = Vector3.Angle(forwardDirection, directionToAttacker);

        return angle > 120f;
    }

    [Command]
    private void CmdOnDamageTaken(Character attacker)
    {
        if (attacker == null) return;
        RpcActivateSkillOnClients(attacker);
    }

    [ClientRpc]
    private void RpcActivateSkillOnClients(Character attacker)
    {
        _lastAttacker = attacker;
        Disactive = false;

        if (_disactiveResetCoroutine != null)
            StopCoroutine(_disactiveResetCoroutine);

        _disactiveResetCoroutine = StartCoroutine(ResetDisactiveAfterDelay());
    }

    private IEnumerator ResetDisactiveAfterDelay()
    {
        yield return new WaitForSeconds(disactiveResetTime);
        _lastAttacker = null;
        Disactive = true;
    }
    
    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
    {
        if (_lastAttacker != null)
        {
            TargetInfo targetInfo = new TargetInfo();
            targetInfo.AddTarget(_lastAttacker);
            targetDataSavedCallback?.Invoke(targetInfo);
        }
        yield break;
    }
    
    protected override IEnumerator CastJob()
    {
        if (Targeting.GetTarget() == null) yield break;

        var target = Targeting.GetTarget().Character;

        Vector3 behindPosition = GetBehindPosition(target);
        
        CmdTeleportPlayer(behindPosition, target.transform.position);

        float fearDuration = Random.Range(2f, 3f);

        CmdAddFear(target, fearDuration);

        if (_isMagicAbilityInstantly)
        {
            ActivateMagicBoostForAll();
        }
        
        Disactive = true;
        _lastAttacker = null;

        yield return null;
    }

    [Command]
    private void CmdAddFear(Character target, float duration)
    {
        target.CharacterState.AddState(States.Fear, duration, 0, Schools.Dark, _hero.gameObject, "RetributiveReckoning");
    }

    private Vector3 GetBehindPosition(Character enemy)
    {
        Vector3 enemyForward = enemy.Move != null ? enemy.Move.transform.forward : enemy.transform.forward;

        Vector3 behindPos = enemy.transform.position - (enemyForward * 1.5f);

        behindPos.y = transform.position.y;
    
        return behindPos;
    }

    private void ActivateMagicBoostForAll()
    {
        if (_magicBoostCoroutine != null)
            StopCoroutine(_magicBoostCoroutine);

        _magicBoostCoroutine = StartCoroutine(MagicBoostWindow());
    }

    private IEnumerator MagicBoostWindow()
    {
        var skills = _hero.Abilities.Skills;

        foreach (var skill in skills)
        {
            if (skill.Info.AbilityForm == AbilityForm.Magic)
                skill.EnableSkillBoost();
        }

        yield return new WaitForSeconds(1f);

        foreach (var skill in skills)
        {
            if (skill.Info.AbilityForm == AbilityForm.Magic)
                skill.DisableSkillBoost();
        }
    }

    [Command]
    private void CmdTeleportPlayer(Vector3 position, Vector3 targetPosition)
    {
        moveComponent.Rigidbody.position = position;

        moveComponent.LookAtPosition(targetPosition);

        RpcTeleportPlayer(position, targetPosition);
    }

    [ClientRpc]
    private void RpcTeleportPlayer(Vector3 position, Vector3 targetPosition)
    {
        if (moveComponent != null)
        {
            moveComponent.Rigidbody.position = position;
            transform.position = position;
            moveComponent.LookAtPosition(targetPosition);
        }

        if (AnimTriggerCast != 0)
        {
            _hero.Animator.SetTrigger(AnimTriggerCast);
        }
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo != null && targetInfo.GetTargets().Count > 0)
        {
            Targeting.SetTarget(targetInfo.GetTargets()[0]);;
        }
    }
}