using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class UndercutSkill : Skill,IComboParticipatingSkill
{
    private float _disappointmentDuration = 3f;
    private float _disappointmentBonusDuration = 0f;

    protected override bool IsCanCast => Targeting.GetTarget() != null &&
                                       Vector3.Distance(Targeting.GetTarget().Transform.position, transform.position) <= AreaInfo.Radius;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("Undercut");

    public void AnimUndercut() => AnimStartCastCoroutine();
    public void AnimUndercutEnd() => AnimCastEnded();

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        while (Targeting.GetTempTarget()?.Character == null)
        {
            if (GetMouseButton)
            {
                Targeting.FindTempTarget(Targeting.GetMousePoint(), 0.5f);

                var temp = Targeting.GetTempTarget()?.Character;
                if (temp != null && temp != Hero)
                    break;
            }
            yield return null;
        }

        Targeting.SetTarget(Targeting.GetTempTarget()?.Character);

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(Targeting.GetTarget()?.Character);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        var target = Targeting.GetTarget()?.Character;
        if (target == null) yield break;

        Damage damage = new Damage
        {
            Value = Buff.Damage.GetBuffedValue(_damageValue),
            Type = DamageType.Physical,
            School = Schools.Physical
        };

        CmdApplyDamage(damage, target.gameObject);

        CmdApplyDisappointment(target.gameObject);

        CommitUse();

        yield return null;
    }

    [Command]
    private void CmdApplyDamage(Damage damage, GameObject target)
    {
        if (target == null) return;
        var damageable = target.GetComponent<IDamageable>();
        var isHit = damageable.TryTakeDamage(ref damage, this);

        if (isHit && damageable is Character character)
        {
            OnDamaged?.Invoke(target,this);
        }
    }

    [Command]
    private void CmdApplyDisappointment(GameObject target)
    {
        if (target == null) return;
        var state = target.GetComponent<CharacterState>();
        state?.AddState(States.DisappointmentState, _disappointmentDuration + _disappointmentBonusDuration, 0f, Hero.gameObject, Name);
        _disappointmentBonusDuration = 0f;
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();
        Targeting.ClearTempTarget();
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0)
            Targeting.SetTarget((ITargetable)targetInfo.GetTargets()[0]);
    }

    public event Action<GameObject, Skill> OnDamaged;
    public void OnFinalComboSkill(GameObject target)
    {
        if (isServer)
            _disappointmentBonusDuration++;
    }

    public void OnTargetHasComboPoint(GameObject target, float comboPoints)
    {
        if (isServer)
        {
            _disappointmentBonusDuration += comboPoints;
        }
    }
}
