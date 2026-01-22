using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gangdollarff.EarthElemental
{
    public class Punch : MoveSkill
    {
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
            SetTarget((ITargetable)(Character)targetInfo.GetTargets()[0]);
        
            if (!IsCanCast)
            {
                MoveTo();
            }
        }

        protected override IEnumerator CastJob()
        {
            Character originalTarget = GetTargetCharacter();
            if (originalTarget == null) yield break;
    
            Hero.Move.LookAtPosition(originalTarget.Position);

            Damage damage = new Damage
            {
                Value = Buff.Damage.GetBuffedValue(Damage),
                Type = DamageType,
                PhysicAttackType = AttackRangeType,
                School = School,
                Form = AbilityForm,
            };
            CmdApplyDamage(damage, originalTarget.gameObject);

            yield return null;
        }

        protected override void ClearData()
        {
            ClearTarget();
            ClearTempTarget();
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
            targetInfo.AddTarget(GetTempTargetCharacter());
            ClearTempTarget();
            targetDataSavedCallback(targetInfo);
        }
    }
}

