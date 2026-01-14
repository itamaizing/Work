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
        
        private float _clickRadius = 0.5f;
        protected override bool IsCanCast => Vector3.Distance(GetTargetCharacter().Position, transform.position) <= Radius;
        private bool IsEnemyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Enemy");

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
            SetTarget((ITargetable)(Character)targetInfo.GetTargets()[0]);
        }

        protected override IEnumerator CastJob()
        {
            Hero.Move.LookAtPosition(GetTargetCharacter().Position);

            Damage damage = new Damage
            {
                Value = Buff.Damage.GetBuffedValue(Damage),
                Type = DamageType,
                PhysicAttackType = AttackRangeType,
                School = School,
                Form = AbilityForm,
             };

            CmdApplyDamage(damage, GetTargetCharacter().gameObject);

            yield return null;
        }

        protected override void ClearData()
        {
            ClearTarget();
            //_target = null;
        }

        protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
        {
            TargetInfo targetInfo = new TargetInfo();
            while (GetTempTarget() == null)
            {
                if (GetMouseButton)
                {
                    Vector3 clickPoint = GetMousePoint();
                
                    FindTarget(_clickRadius, clickPoint, canTargetHimself: false);

                    if (GetTempTargetCharacter() is Character character)
                    {
                        if (GetTempTargetCharacter() != null && !IsEnemyTarget(character))
                        {
                            ClearTempTarget();
                        }
                        else
                        {
                            if (character.SelectedCircle != null) character.SelectedCircle.IsActive = false;
                            break;
                        }
                    }
                }
                yield return null;
            }
            SetTarget(GetTempTarget());
            ClearTempTarget();
            targetInfo.AddTarget(GetTargetCharacter());
            targetDataSavedCallback(targetInfo);
        }
    }
}

