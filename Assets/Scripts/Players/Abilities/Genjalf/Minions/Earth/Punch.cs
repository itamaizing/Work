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
        protected override bool IsCanCast => Vector3.Distance(Targeting.GetTarget().Character.Position, transform.position) <= AreaInfo.Radius;

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
            Targeting.SetTarget((ITargetable)(Character)targetInfo.GetTargets()[0]);
        }

        protected override IEnumerator CastJob()
        {
            Hero.Move.LookAtPosition(Targeting.GetTarget().Character.Position);

            Damage damage = new Damage
            {
                Value = Buff.Damage.GetBuffedValue(Damage),
                Type = DamageType,
                PhysicAttackType = AttackRangeType,
                School = School,
                Form = AbilityForm,
             };

            CmdApplyDamage(damage, Targeting.GetTarget().Character.gameObject);

            yield return null;
        }

        protected override void ClearData()
        {
            Targeting.ClearTarget();
            //_target = null;
        }

        protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
        {
            Character target = null;

            TargetInfo targetInfo = new();

            while (Targeting.GetTarget().Character == null)
            {
                if (GetMouseButton)
                    Targeting.FindTempTarget();
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

