using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Gangdollarff
{
    public class FireworkDsplay : Skill
    {
        [SerializeField] private Firework _firework;
        [SerializeField] private float _damageRangeMin = -2;
        [SerializeField] private float _damageRangeMax = 1;

        private bool _isBlinding;
        
        private bool IsEnemyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Enemy");

        private Vector3 _targetPoint;
        private float _blindingChance = 50;
        private float _blindingDuration = 2f;

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
            _targetPoint = targetInfo.Points[0];
        }

        public void SetBlinding(bool isBlinding)
        {
            _isBlinding = isBlinding;
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
                        if (!item.TryGetComponent<IDamageable>(out var enemy))
                            continue;
                        
                        if (!item.TryGetComponent<Character>(out var character) || !IsEnemyTarget(character))
                            continue;
                        
                        float modifier = 1f - (0.25f * index);
                        if (modifier <= 0f)
                            break;
                        float currentDamage =
                            Random.Range(Damage + _damageRangeMin, Damage + _damageRangeMax);
                        currentDamage *= modifier;
                        Damage damage = new Damage
                        {
                            Value = Buff.Damage.GetBuffedValue(currentDamage),
                            Type = DamageType,
                            PhysicAttackType = AttackRangeType,
                        };
                        CmdApplyDamage(damage, item.gameObject);
                        if (_isBlinding)
                        {
                            var randomInt = Random.Range(0, 100);

                            if (randomInt > _blindingChance)
                            {
                                CmdAddState(item.gameObject);
                            }
                        }
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

            Targeting.ClearTarget();
            Targeting.ClearTempTarget();
           // _target = null;
            _targetPoint = Vector3.positiveInfinity;
        }

        protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
        {
            TargetInfo targetInfo = new();
            while (!Input.GetMouseButtonDown(0))
                yield return null;

            _targetPoint = Targeting.GetMousePoint();
            targetInfo.Points.Add(_targetPoint);
            callbackDataSaved(targetInfo);
        }

        private void EnableMove()
        {
            Hero.Animator.SetTrigger(HashAnimPlayer.AnimCancled);
            Hero.Move.StopLookAt();
            Hero.Move.IsLookAtCursor = true;
            
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
        private void CmdAddState(GameObject target)
        {
            target.GetComponent<Character>().CharacterState.AddState(States.Blind,_blindingDuration,0,_hero.gameObject,nameof(FireworkDsplay));
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

