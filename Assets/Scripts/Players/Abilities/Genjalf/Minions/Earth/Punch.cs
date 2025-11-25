using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gangdollarff.EarthElemental
{
    public class Punch : Skill
    {

        //private Character _target;

        protected override int AnimTriggerCastDelay => 0;
        protected override int AnimTriggerCast => Animator.StringToHash("Attack01");
        protected override bool IsCanCast => Vector3.Distance(GetTarget().Position, transform.position) <= Radius;

        public void AnimCastPunch()
        {
            AnimStartCastCoroutine();
        }

        public void AnimPunchEnd()
        {
            AnimCastEnded();
        }   

        public override void LoadTargetData(TargetInfo targetInfo)
        {
            SetTarget((Character)targetInfo.GetTargets()[0]);
        }

        protected override IEnumerator CastJob()
        {
            Hero.Move.LookAtPosition(GetTarget().Position);

            Damage damage = new Damage
            {
                Value = Buff.Damage.GetBuffedValue(Damage),
                Type = DamageType,
                PhysicAttackType = AttackRangeType,
                School = School,
                Form = AbilityForm,
             };

            CmdApplyDamage(damage, GetTarget().gameObject);

            yield return null;
        }

        protected override void ClearData()
        {
            ClearTarget();
            //_target = null;
        }

        protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
        {
            Character target = null;

            TargetInfo targetInfo = new();

            while (GetTarget() == null)
            {
                if (GetMouseButton)
                    FindTarget();
               //     target = GetRaycastTarget();

                yield return null;
            }

            Hero.Move.LookAtPosition(target.Position);
            targetInfo.AddTarget(target);
            targetDataSavedCallback.Invoke(targetInfo);
            yield return null;
        }
    }
}

