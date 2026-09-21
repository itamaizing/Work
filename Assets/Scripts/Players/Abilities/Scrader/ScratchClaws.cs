using System;
using System.Collections;
using UnityEngine;

public class ScratchClaws : MoveSkill
{
    [SerializeField] private float minDamage = 1f;
    [SerializeField] private float maxDamage = 4f;
    [SerializeField] private float bleedingDuration = 3f;
    [SerializeField, Range(0, 1f)] private float bleedingChance = 1f;
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("AttackScared");

    protected override bool IsCanCast
    {
        get
        {
            var target = Targeting.GetTarget()?.Character;
            if (target == null) return false;
            return Vector3.Distance(target.Position, transform.position) <= AreaInfo.Radius;
        }
    }

    public void AnimCastScratch()
    {
        AnimStartCastCoroutine();
    }

    public void AnimScratchEnd()
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
        if (target == null)
        {
            yield break;
        }

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

        CmdApplyDamage(damage, target.gameObject);

        if (UnityEngine.Random.value <= bleedingChance)
            target.CharacterState.CmdAddState(States.Bleeding, bleedingDuration, 1f, Hero.gameObject, Name);
        
        yield return null;
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();
        Targeting.ClearTempTarget();
        AnimScratchEnd();
    }
}
