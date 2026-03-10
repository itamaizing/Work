using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Gangdollarff.EarthElemental
{
    public class Punch : MoveSkill
    {
        [SerializeField] private float _stunDuration = 1.5f;
        [SerializeField] private float _stunChance = 0.15f;
        
        protected override int AnimTriggerCastDelay => 0;
        protected override int AnimTriggerCast => Animator.StringToHash("Attack01");
        
        private float _clickRadius = 0.5f;
        protected override bool IsCanCast
        {
            get
            {
                var target = Targeting.GetTarget()?.Character;
                if (target == null) return false;
                return Vector3.Distance(target.Position, transform.position) <= AreaInfo.Radius;
            }
        }
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
            if (targetInfo.GetTargets().Count == 0) return;

            
            Targeting.SetTarget((ITargetable)(Character)targetInfo.GetTargets()[0]);
        
            if (!IsCanCast)
            {
                MoveTo();
            }
        }

        protected override IEnumerator CastJob()
        {
            Character originalTarget = Targeting.GetTarget()?.Character;
            if (originalTarget == null) yield break;
    
            Hero.Move.LookAtPosition(originalTarget.Position);

            Damage damage = new Damage
            {
                Value = Buff.Damage.GetBuffedValue(Damage),
                Type = Info.DamageType,
                PhysicAttackType = Info.AttackRangeType,
                School = Info.School,
                Form = Info.AbilityForm,
             };

            CmdApplyDamage(damage, Targeting.GetTarget()?.Character.gameObject);
            CmdAddState(originalTarget.gameObject);

            yield return null;
        }

        protected override void ClearData()
        {
            Targeting.ClearTarget();
            Targeting.ClearTempTarget();
            //_target = null;
        }

        protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
        {
            Character target = null;

            TargetInfo targetInfo = new();

            while (Targeting.GetTempTarget()?.Character == null)
            {
                if (GetMouseButton)
                {
                    Vector3 clickPoint = Targeting.GetMousePoint();
        
                    Targeting.FindTempTarget(clickPoint, _clickRadius, canTargetSelf: false);
                    if (Targeting.GetTempTarget()?.Character is Character character)
                    {
                        if (Targeting.GetTempTarget() != null && !IsEnemyTarget(character))
                        {
                            Targeting.ClearTempTarget();
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
            targetInfo.AddTarget(Targeting.GetTempTarget()?.Character);
            Targeting.ClearTempTarget();
            targetDataSavedCallback(targetInfo);
        }

        [Command]
        private void CmdAddState(GameObject target)
        {
            if(target == null) return;

            if (target == null) return;
            
            if (UnityEngine.Random.value > _stunChance)
                return;

            if (target.TryGetComponent(out Character enemy))
            {
                enemy.CharacterState.AddState(States.Stun, _stunDuration, 0, _hero.gameObject, nameof(Punch));
            }
        }
    }
}

