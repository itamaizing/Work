using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gangdollarff
{
    public class AbsorptionBall : Skill, IGodLightSpell
    {
        [SerializeField] private Shield _shieldPref;
        [SerializeField] private float _shieldValue = 40;
        [SerializeField] private float _shieldDuration = 2;

        private float _tempCooldownTime = 5f;

        private float _absorbedDamage;

        private float _absorbationMultiplier;

        private Shield _shield;

        public override string AdditionalDescription =>
            $"Ёффективность: {AbilityNameBox.ColorOpen}{_shieldValue} ед.{AbilityNameBox.ColorEnd}" +
            $"\nƒлительность: {AbilityNameBox.ColorOpen}{_shieldDuration} сек{AbilityNameBox.ColorEnd}";

        protected override int AnimTriggerCastDelay => 0;

        protected override int AnimTriggerCast => 0;

        protected override bool IsCanCast => true;
        
        public float ShieldDuration { get => _shieldDuration; set => _shieldDuration = value; }
        
        private float _clickRadius = 0.5f;
        private bool IsAllyTarget(IDamageable target) => target.gameObject.layer == LayerMask.NameToLayer("Allies");

        public float AbsorbationMultiplier { get => _absorbationMultiplier; set => _absorbationMultiplier = value; }
        public bool IsEnabled { get; protected set; }
        public bool IsAllyTargetAvailable = false;

        public override void LoadTargetData(TargetInfo targetInfo)
        {
        }

        public void ChangeMode()
        {
            if (IsEnabled)
            {
                IsEnabled = false;

                _cooldownTime = _tempCooldownTime;
            }
            else
            {
                IsEnabled = true;

                _tempCooldownTime = _cooldownTime;
                _cooldownTime = 0;
            }
        }

        protected override IEnumerator CastJob()
        {
            CmdAddShield(IsAllyTargetAvailable ? GetTargetCharacter().gameObject : Hero.gameObject);
            yield return null;
        }

        protected override void ClearData()
        {
        }

        private void ClearSkillInfo()
        {
            _absorbedDamage = 0;
        }

        protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
        {
            if(!IsAllyTargetAvailable) yield break;
            
            TargetInfo targetInfo = new TargetInfo();

            while (GetTempTarget() == null)
            {
                if (GetMouseButton)
                {
                    Vector3 clickPoint = GetMousePoint();
                
                    FindTarget(_clickRadius, clickPoint, canTargetHimself: true);

                    if (GetTempTargetCharacter() is Character character)
                    {
                        if (GetTempTargetCharacter() != null && !IsAllyTarget(character))
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
            SetTarget(GetTempTargetCharacter());
            ClearTempTarget();

            targetInfo.AddTarget(GetTargetCharacter());
            callbackDataSaved(targetInfo);
        }

        [Command]
        private void CmdAddShield(GameObject target)
        {
            if(target == null) return;
            if (_shield != null)
            {
                NetworkServer.Destroy(_shield.gameObject);
            }

            Character targetChar = target.GetComponent<Character>();
            
            var shield = Instantiate(_shieldPref, target.transform.position, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(shield.gameObject, _hero.NetworkSettings.MyRoom);
            shield.Initialize(_shieldValue, DamageType.Both);
            NetworkServer.Spawn(shield.gameObject);
            _shield = shield;
            targetChar.Health.Shields.Add(shield);
            StartCoroutine(ShieldJob(target));

            ClientRpcShieldFollow(_shield.gameObject, target.transform);
        }

        [ClientRpc]
        private void ClientRpcShieldFollow(GameObject shield, Transform target)
        {
            shield.GetComponent<Shield>().FollowTo(target);
        }

        private void OnAbsorb(Damage damage, Skill skill)
        {
            _absorbedDamage += damage.Value;
        }

        private IEnumerator ShieldJob(GameObject target)
        {
            _shield.DamageTaken += OnAbsorb;
            yield return new WaitForSecondsRealtime(_shieldDuration);
            _shield.DamageTaken -= OnAbsorb;
            RpcAbsorbJob(target,_absorbedDamage);
            if (_shield != null)
            {
                _shield.TryUse(99999);
                NetworkServer.Destroy(_shield.gameObject);
            }
            ClearSkillInfo();
        }

        [TargetRpc]
        private void RpcAbsorbJob(GameObject target,float damage)
        {
            if (target != null)
            {
                target.TryGetComponent(out Character character);
                character.TryGetResource(ResourceType.Mana).CmdAdd(damage * _absorbationMultiplier);
            }
        }
    }
}