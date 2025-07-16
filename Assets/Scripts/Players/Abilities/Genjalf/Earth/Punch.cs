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
            throw new NotImplementedException();
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
                    target = GetRaycastTarget();

                yield return null;
            }

            targetInfo.Targets.Add(target);
            targetDataSavedCallback.Invoke(targetInfo);
            yield return null;
        }
    }
}

