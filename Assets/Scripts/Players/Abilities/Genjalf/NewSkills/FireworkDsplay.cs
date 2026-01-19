using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gangdollarff
{
    public class FireworkDsplay : Skill
    {
        [SerializeField] private Firework _firework;
        [SerializeField] private float _damageRangeMin = -2;
        [SerializeField] private float _damageRangeMax = 1;
        [SerializeField] private float _rotationSpeed = 10f;


        private List<float> _damageForTarget = new List<float>() { 1, .75f, .50f, .25f };

        private Vector3 _targetPoint = Vector3.positiveInfinity;
        //private Character _target;

        private float _clickRadius = 0.5f;
        protected override int AnimTriggerCastDelay => 0;

        protected override int AnimTriggerCast => 0;

        protected override bool IsCanCast { get => CheckCanCast(); }

        private bool CheckCanCast()
        {
            return true;
        }

        public override void LoadTargetData(TargetInfo targetInfo)
        {
            SetTarget((ITargetable)(Character)targetInfo.GetTargets()[0]);
            _targetPoint = targetInfo.Points[0];
        }

        protected override IEnumerator CastJob()
        {
            CmdSetActiveParticle(true);
            float elapsedTime = 0f;
            float manaTimer = 0f;
            Hero.Move.RotateModifier = 0.05f;
            DisableMove();
            while (elapsedTime < CastStreamDuration)
            {
                float delta = Time.deltaTime;
                elapsedTime += delta;
                manaTimer += delta;
                if (manaTimer >= _manaCostRate)
                {
                    manaTimer -= _manaCostRate;
                    _firework.SortDamageablesByDistance(transform.position);
                    int index = 0;
                    foreach (var item in _firework.Damageables)
                    {
                        if (((1 << item.gameObject.layer) & TargetsLayers) == 0)
                            continue;
                        if (!item.TryGetComponent<IDamageable>(out var enemy))
                            continue;
                        float modifier = 1f - (0.25f * index);
                        if (modifier <= 0f)
                            break;
                        float currentDamage =
                            UnityEngine.Random.Range(Damage + _damageRangeMin, Damage + _damageRangeMax);
                        currentDamage *= modifier;
                        Damage damage = new Damage
                        {
                            Value = Buff.Damage.GetBuffedValue(currentDamage),
                            Type = DamageType,
                            PhysicAttackType = AttackRangeType,
                        };
                        CmdApplyDamage(damage, item.gameObject);
                        index++;
                    }
                }

                yield return null;
            }

            ClearData();
        }


        protected override void ClearData()
        {
            //Hero.Move.RotateModifier = 0;
            EnableMove();
            _firework.gameObject.SetActive(false);
            CmdSetActiveParticle(false);

            ClearTarget();
            ClearTempTarget();
           // _target = null;
            _targetPoint = Vector3.positiveInfinity;
        }

        protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
        {
            while (GetTempTargetCharacter() == null)
            {
                if (GetMouseButton)
                {
                    Vector3 clickPoint = GetMousePoint();

                    FindTarget(_clickRadius, clickPoint, canTargetHimself: true);
                }
                yield return null;
            }
            TargetInfo targetInfo = new();
            SetTarget(GetTempTargetCharacter());
            targetInfo.AddTarget(GetTargetCharacter());
            targetInfo.Points.Add(_targetPoint);
            callbackDataSaved(targetInfo);
        }

        private void EnableMove()
        {
            Hero.Animator.SetTrigger(HashAnimPlayer.AnimCancled);
            Hero.Move.IsMoveBlocked = false;
            Hero.Move.RotateModifier = 1f;
        }

        private void DisableMove()
        {
            Hero.Animator.SetTrigger("Fire");
            Hero.Move.IsMoveBlocked = true;
            Hero.Move.StopLookAt();
        }

        [Command]
        private void CmdSetActiveParticle(bool status)
        {
            ClientRpcSetActiveParticle(status);
        }

        [ClientRpc]
        private void ClientRpcSetActiveParticle(bool status)
        {
            _firework.gameObject.SetActive(status);
        }
    }
}

