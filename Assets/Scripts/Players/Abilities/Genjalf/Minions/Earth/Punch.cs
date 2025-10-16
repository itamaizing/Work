using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gangdollarff.EarthElemental
{
    public class Punch : Skill
    {

        private Character _target;

        protected override int AnimTriggerCastDelay => 0;
        protected override int AnimTriggerCast => Animator.StringToHash("Attack01");
        protected override bool IsCanCast => Vector3.Distance(_target.Position, transform.position) <= Radius;

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
            _target = (Character)targetInfo.Targets[0];
        }

        protected override IEnumerator CastJob()
        {
            Hero.Move.LookAtPosition(_target.Position);

            Damage damage = new Damage
            {
                Value = Buff.Damage.GetBuffedValue(Damage),
                Type = DamageType,
                PhysicAttackType = AttackRangeType,
                School = School,
                Form = AbilityForm,
             };

            CmdApplyDamage(damage, _target.gameObject);

            yield return null;
        }

        protected override void ClearData()
        {
            _target = null;
        }

        protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
        {
            Character target = null;

            TargetInfo targetInfo = new();

            while (target == null)
            {
                if (GetMouseButton)
               //     target = GetRaycastTarget();

                yield return null;
            }

            Hero.Move.LookAtPosition(target.Position);
            targetInfo.Targets.Add(target);
            targetDataSavedCallback.Invoke(targetInfo);
            yield return null;
        }
    }
}

