using System;
using System.Collections;
using Mirror;
using UnityEngine;

namespace Gangdollarff.AirElemental
{
    public class Light : MoveSkill
    {
        [SerializeField] private ParticleSystem _particlePref;
        [SerializeField, Range(0, 100)] private int _debuffChance = 30;

        protected override bool IsCanCast { get => CheckCanCast(); }
        protected override int AnimTriggerCastDelay => 0;
        protected override int AnimTriggerCast => Animator.StringToHash("AttackLight");
        private bool IsEnemyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Enemy");
        
        private float _clickRadius = 0.5f;
        private float _particleLifetime = 1f;

        private void OnEnable()
        {
            Canceled += CancelMove;
        }

        private void OnDisable()
        {
            Canceled -= CancelMove;
        }
        
        private bool CheckCanCast()
        {
            return Vector3.Distance(Targeting.GetTarget().Character.transform.position, transform.position) <= AreaInfo.Radius;
        }

        public void AnimCastLight()
        {
            AnimStartCastCoroutine();
        }

        public void AnimLightEnd()
        {
            AnimCastEnded();
        }

        public override void LoadTargetData(TargetInfo targetInfo)
        {
            Targeting.SetTarget((ITargetable)(Character)targetInfo.GetTargets()[0]);
            if (!IsCanCast)
            {
                MoveTo();
            }
        }

        protected override IEnumerator CastJob()
        {
            if (Targeting.GetTarget()?.Character != null)
            {
                var target = Targeting.GetTarget()?.Character;
                Damage damage = new Damage
                {
                    Value = Buff.Damage.GetBuffedValue(Damage),
                    Type = Info.DamageType,
                    PhysicAttackType = Info.AttackRangeType,
                };
                CmdApplyDamage(damage, target.gameObject);

                CmdCreateParticle(target.Position);

                if (UnityEngine.Random.Range(1, 100) <= _debuffChance)
                {
                    CmdAddState(target.gameObject);
                }
            }
            yield return null;
        }

        protected override void ClearData()
        {
            Targeting.ClearTarget();
            //_target = null;
        }

        protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
        {
            TargetInfo targetInfo = new TargetInfo();
            while (Targeting.GetTempTarget() == null)
            {
                if (GetMouseButton)
                {
                    Vector3 clickPoint = Targeting.GetMousePoint();
        
                    Targeting.FindTempTarget(clickPoint, _clickRadius, canTargetSelf: false);
                    if (Targeting.GetTempTarget()?.Character is Character character)
                    {
                        if (Targeting.GetTempTarget()?.Character != null && !IsEnemyTarget(character))
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
            callbackDataSaved(targetInfo);
        }

        private void CreateParticle(Vector3 position)
        {
            Destroy(Instantiate(_particlePref.gameObject, position, Quaternion.identity),_particleLifetime);
        }

        [Command]
        private void CmdAddState(GameObject target)
        {
            if (target.TryGetComponent(out Character character))
            {
                character.CharacterState.AddState(States.Discharge, 2, 0,Schools.Air, Hero.gameObject, name);
            }
        }

        [Command]
        protected void CmdCreateParticle(Vector3 position)
        {
            RpcCreateParticle(position);
        }

        [ClientRpc]
        private void RpcCreateParticle(Vector3 position)
        {
            CreateParticle(position);
        }
    }
}
