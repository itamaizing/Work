using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class SpittingAcid : MoveSkill
{
    [SerializeField] private float minDamage = 8f;
    [SerializeField] private float maxDamage = 10f;
    [SerializeField] private float corrodedDuration = 6f;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("SpittingAcid");

    protected override bool IsCanCast
    {
        get
        {
            var target = Targeting.GetTarget()?.Character;
            if (target == null) return false;
            return Vector3.Distance(target.Position, transform.position) <= AreaInfo.Radius;
        }
    }

    public void AnimCastAcid()
    {
        AnimStartCastCoroutine();
    }

    public void AnimAcidEnd()
    {
        AnimCastEnded();
    }

    private void OnEnable()
    {
        Canceled += CancelMove;
    }

    private void OnDisable()
    {
        Canceled -= CancelMove;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0) Targeting.SetTarget(targetInfo.GetTargets()[0]);

        if (!IsCanCast)
        {
            MoveTo();
        }
    }

    protected override IEnumerator CastJob()
    {
        Character target = Targeting.GetTarget()?.Character;
        if (target == null) yield break;

        Hero.Move.LookAtPosition(target.Position);

        float dmgValue = UnityEngine.Random.Range(minDamage, maxDamage);

        Damage damage = new Damage
        {
            Value = Buff.Damage.GetBuffedValue(dmgValue),
            Type = Info.DamageType,
            PhysicAttackType = Info.AttackRangeType,
            School = Info.School,
            Form = Info.AbilityForm,
        };
        CmdAddCorrodedArmor(target.gameObject);
        CmdApplyDamage(damage, target.gameObject);
        
        ClearData();

        yield return null;
    }

    [Command]
    private void CmdAddCorrodedArmor(GameObject target)
    {
        if(target == null) return;
        target.GetComponent<CharacterState>().AddState(States.CorrodedArmor, corrodedDuration, 0f, Hero.gameObject, Name);
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();
        Targeting.ClearTempTarget();
        AnimAcidEnd();
    }
}