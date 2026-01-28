using Mirror;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gangdollarff
{
    public class AbsorptionBall : Skill, IGodLightSpell
    {
        [SerializeField] private Shield _shieldPref;
        [SerializeField] private float _shieldValue = 40;
        [SerializeField] private float _shieldDuration = 2;

        private Shield _shield;
        
        private float _tempCooldownTime = 5f;

        private float _absorbedDamage;

        private float _absorbationMultiplier = 2f;
        
        private float _lastOpacityChangeTime = 0f;
        
        public override string AdditionalDescription =>
            $"Ёффективность: {AbilityNameBox.ColorOpen}{_shieldValue} ед.{AbilityNameBox.ColorEnd}" +
            $"\nƒлительность: {AbilityNameBox.ColorOpen}{_shieldDuration} сек{AbilityNameBox.ColorEnd}";

        protected override int AnimTriggerCastDelay => 0;

        protected override int AnimTriggerCast => 0;

        protected override bool IsCanCast => CheckCanCast();

        public float ShieldDuration { get => _shieldDuration; set => _shieldDuration = value; }
        
        private float _clickRadius = 0.5f;
        private bool IsAllyTarget(IDamageable target) => target.gameObject.layer == LayerMask.NameToLayer("Allies");
        
        private bool CheckCanCast()
        {
            if(IsAllyTargetAvailable)
                return Vector3.Distance(GetTargetCharacter().transform.position, transform.position) <= Radius && GetTargetCharacter() != null;
            return true;
        }
        public bool IsEnabled { get; protected set; }
        public bool IsAllyTargetAvailable = false;
        public bool IsManaRegenActive = false;

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
            CmdAddShield(IsAllyTargetAvailable ? GetTargetCharacter().gameObject : Hero.gameObject, Hero.gameObject);
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
        private void CmdAddShield(GameObject targetShield,GameObject targetAbsorb)
        {
            if(targetShield == null) return;
            if (_shield != null)
            {
                NetworkServer.Destroy(_shield.gameObject);
            }

            Character targetChar = targetShield.GetComponent<Character>();
            
            var shield = Instantiate(_shieldPref, targetShield.transform.position, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(shield.gameObject, _hero.NetworkSettings.MyRoom);
            shield.Initialize(_shieldValue, DamageType.Both);
            NetworkServer.Spawn(shield.gameObject);
            _shield = shield;
            targetChar.Health.Shields.Add(shield);
            StartCoroutine(ShieldJob(targetAbsorb));

            ClientRpcShieldFollow(_shield.gameObject, targetShield.transform);
        }

        [ClientRpc]
        private void ClientRpcShieldFollow(GameObject shield, Transform target)
        {
            shield.GetComponent<Shield>().FollowTo(target);
        }

        private void OnAbsorb(Damage damage, Skill skill)
        {
            _absorbedDamage += damage.Value;
            
            RpcManaSpendJob(_hero.gameObject,damage.Value);

            if (_shield)
            {
                RpcChangeShieldAlpha(_shield.gameObject);
            }
        }

        private IEnumerator ShieldJob(GameObject target)
        {
            _shield.DamageTaken += OnAbsorb;
            yield return new WaitForSecondsRealtime(_shieldDuration);
            _shield.DamageTaken -= OnAbsorb;
            
            if(IsManaRegenActive)
                RpcAbsorbJob(target,_absorbedDamage);
            
            if (_shield != null)
            {
                _shield.TryUse(99999);
                NetworkServer.Destroy(_shield.gameObject);
            }
            ClearSkillInfo();
        }

        [TargetRpc]
        private void RpcManaSpendJob(GameObject target, float count)
        {
            if (target != null)
            {
                target.TryGetComponent(out Character character);
                character.TryGetResource(ResourceType.Mana).CmdUse(count);
            }
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
        
        [ClientRpc]
        private void RpcChangeShieldAlpha(GameObject shieldGo)
        {
            if (shieldGo == null) return;

            _lastOpacityChangeTime = Time.time;

            var renderer = shieldGo.GetComponentInChildren<ParticleSystemRenderer>();
            if (renderer == null) return;

            var mat = renderer.material;
            var col = mat.color;

            col.a += 0.2f;
            mat.color = col;
        }
    }
}