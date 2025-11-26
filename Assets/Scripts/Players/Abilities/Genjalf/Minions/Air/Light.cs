using System;
using System.Collections;
using Mirror;
using UnityEngine;

namespace Gangdollarff.AirElemental
{
    public class Light : Skill
    {
        [SerializeField] private ParticleSystem _particlePref;
        [SerializeField, Range(0, 100)] private int _debuffChance = 15;

        //private Character _target;

        protected override bool IsCanCast { get => CheckCanCast(); }

        protected override int AnimTriggerCastDelay => 0;

        protected override int AnimTriggerCast => Animator.StringToHash("AttackLight");

        private bool CheckCanCast()
        {
            return
                   Vector3.Distance(GetTarget().transform.position, transform.position) <= Radius;
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
            SetTarget((Character)targetInfo.GetTargets()[0]);
        }

        protected override IEnumerator CastJob()
        {
            if (GetTarget() != null)
            {
                Damage damage = new Damage
                {
                    Value = Buff.Damage.GetBuffedValue(Damage),
                    Type = DamageType,
                    PhysicAttackType = AttackRangeType,
                };
                CmdApplyDamage(damage, GetTarget().gameObject);

                CmdCreateParticle(GetTarget().Position);

                if (UnityEngine.Random.Range(1, 100) <= _debuffChance)
                {
					GetTarget().CharacterState.AddState(States.Discharge, 2, 0, Hero.gameObject, name);
                }
            }
            yield return null;
        }

        protected override void ClearData()
        {
            ClearTarget();
            //_target = null;
        }

        protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
        {
            TargetInfo targetInfo = new TargetInfo();

            while (GetTarget() == null)
            {
                if (GetMouseButton)
                {
                    FindTarget();
               //     _target = GetRaycastTarget();
                }
                yield return null;
            }

            targetInfo.AddTarget(GetTarget());
            callbackDataSaved(targetInfo);
        }

        private void CreateParticle(Vector3 position)
        {
            GameObject item = Instantiate(_particlePref.gameObject, position, Quaternion.identity);
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