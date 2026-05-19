using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Gangdollarff
{
    public class AbsorptionBall : Skill
    {
        [SerializeField] private Shield _shieldPref;
        [SerializeField] private float _shieldValue = 100;
        [SerializeField] private float _shieldDuration = 2;
        [SerializeField] private float _aoeRadius = 1f;
        [SerializeField] private float _aoeCheckInterval = 0.1f;
        [SerializeField] private Vector3 _aoeShieldScale = new Vector3(1.5f, 1.5f, 1.5f);

        private Shield _shield;
        private Character _mainShieldTarget;
        
        private Coroutine _aoeScanCoroutine;
        
        private Transform _mainShieldTargetTransform;

        private float _absorbedDamage;
        private float _absorbationMultiplier = 2f;
        private float _lastOpacityChangeTime = 0f;
        private bool _shieldBroken = false;

        private float _damagePerSecond;
        private float _lastDamageTime;

        private readonly HashSet<Character> _aoeShieldTargets = new HashSet<Character>();

        private float _clickRadius = 0.5f;

        public float ShieldDuration { get => _shieldDuration; set => _shieldDuration = value; }
        public bool IsAllyTargetAvailable = false;
        public bool IsManaRegenActive = false;
        private bool _isAoeShieldActive = false;

        public override string AdditionalDescription => "";

        protected override int AnimTriggerCastDelay => 0;
        protected override int AnimTriggerCast => 0;
        protected override bool IsCanCast => CheckCanCast();

        public void EnableAoeShieldTalent(bool value)
        {
            if(value == _isAoeShieldActive) return;

            _isAoeShieldActive = value;
            CmdEnableAoeShieldTalent(value);
        }

        [Command]
        private void CmdEnableAoeShieldTalent(bool value)
        {
            _isAoeShieldActive = value;
        }
        
        private bool IsAllyTarget(IDamageable target) =>
            target.gameObject.layer == LayerMask.NameToLayer("Allies");

        private bool CheckCanCast()
        {
            if (IsAllyTargetAvailable)
                return Targeting.GetTarget()?.Character != null &&
                       Vector3.Distance(Targeting.GetTarget().Character.transform.position, transform.position) <= AreaInfo.Radius;
            return true;
        }

        public override void LoadTargetData(TargetInfo targetInfo) { }

        protected override IEnumerator CastJob()
        {
            var target = IsAllyTargetAvailable
                ? Targeting.GetTarget()?.Character.gameObject
                : Hero.gameObject;

            CmdAddShield(target, Hero.gameObject);
            yield return null;
        }

        protected override void ClearData()
        {
            base.ClearData();
            _hero.Move.SetCanMove(true);
        }

        private void ClearSkillInfo()
        {
            _absorbedDamage = 0;
            _shieldBroken = false;
            _aoeShieldTargets.Clear();
        }

        protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
        {
            if (!IsAllyTargetAvailable) yield break;

            TargetInfo targetInfo = new TargetInfo();

            while (Targeting.GetTempTarget() == null)
            {
                if (GetMouseButton)
                {
                    Vector3 clickPoint = Targeting.GetMousePoint();
                    Targeting.FindTempTarget(clickPoint, _clickRadius, canTargetSelf: true);

                    if (Targeting.GetTempTarget()?.Character is Character character)
                    {
                        if (!IsAllyTarget(character))
                            Targeting.ClearTempTarget();
                        else
                        {
                            if (character.SelectedCircle != null)
                                character.SelectedCircle.IsActive = false;
                            break;
                        }
                    }
                }
                yield return null;
            }

            Targeting.SetTarget(Targeting.GetTempTarget()?.Character);
            Targeting.ClearTempTarget();
            targetInfo.AddTarget(Targeting.GetTarget()?.Character);
            callbackDataSaved(targetInfo);
        }

        [Command]
        private void CmdAddShield(GameObject targetShield, GameObject targetAbsorb)
        {
            if (targetShield == null) return;

            if (_shield != null)
                NetworkServer.Destroy(_shield.gameObject);

            _mainShieldTarget = targetShield.GetComponent<Character>();
            if (_mainShieldTarget == null) return;

            var shield = Instantiate(_shieldPref, targetShield.transform.position, Quaternion.identity);
            shield.Initialize(_shieldValue, DamageType.Both);
            NetworkServer.Spawn(shield.gameObject);
            _shield = shield;

            _mainShieldTarget.Health.Shields.Add(_shield);

            StartCoroutine(ShieldJob(targetAbsorb));
            ClientRpcShieldFollow(_shield.gameObject, targetShield.transform);

            if (_isAoeShieldActive)
            {
                ClientRpcSetShieldScale(_shield.gameObject, _aoeShieldScale);
                TargetRpcStartAoeScan(connectionToClient, targetShield);
            }
        }

        [ClientRpc]
        private void ClientRpcShieldFollow(GameObject shield, Transform target)
        {
            shield.GetComponent<Shield>().FollowTo(target);
        }

        [ClientRpc]
        private void ClientRpcSetShieldScale(GameObject shieldGo, Vector3 scale)
        {
            if (shieldGo != null)
                shieldGo.transform.localScale = scale;
        }

        private IEnumerator ShieldJob(GameObject targetAbsorb)
        {
            RpcControlMovement(false);
            _shield.DamageTaken += OnAbsorb;

            yield return new WaitForSeconds(_castDuration);

            if (_isAoeShieldActive)
                TargetRpcStopAoeScan(connectionToClient);

            if (_shield != null)
                _shield.DamageTaken -= OnAbsorb;

            RemoveAoeShields();

            if (IsManaRegenActive && !_shieldBroken)
                RpcAbsorbJob(targetAbsorb, _absorbedDamage);

            if (_shield != null)
            {
                _shield.TryUse(99999);
                NetworkServer.Destroy(_shield.gameObject);
                _shield = null;
            }

            RpcControlMovement(true);
            ClearSkillInfo();
        }
        
        [TargetRpc]
        private void TargetRpcStartAoeScan(NetworkConnection conn, GameObject mainTarget)
        {
            _mainShieldTargetTransform = mainTarget != null ? mainTarget.transform : null;
            _aoeScanCoroutine = StartCoroutine(ClientAoeScanJob());
        }
        
        [TargetRpc]
        private void TargetRpcStopAoeScan(NetworkConnection conn)
        {
            if (_aoeScanCoroutine != null)
            {
                StopCoroutine(_aoeScanCoroutine);
                _aoeScanCoroutine = null;
            }
            _mainShieldTargetTransform = null;
        }

        private IEnumerator ClientAoeScanJob()
        {
            while (_mainShieldTargetTransform != null)
            {
                var hits = Physics.OverlapSphere(
                    _mainShieldTargetTransform.position,
                    _aoeRadius
                );

                List<uint> netIds = new();

                foreach (var col in hits)
                {
                    if (col.gameObject == _mainShieldTargetTransform.gameObject)
                        continue;

                    if (col.TryGetComponent<Character>(out var character))
                    {
                        if (!character.IsDead && IsAllyTarget(character))
                            netIds.Add(character.netId);
                    }
                }

                CmdUpdateAoeTargets(netIds);
                yield return new WaitForSeconds(_aoeCheckInterval);
            }
        }

        [Command]
        private void CmdUpdateAoeTargets(List<uint> netIds)
        {
            if (_mainShieldTarget == null || _shield == null)
                return;
            
            HashSet<Character> newTargets = new();

            foreach (var id in netIds)
            {
                if (!NetworkServer.spawned.TryGetValue(id, out var identity))
                    continue;

                if (!identity.TryGetComponent<Character>(out var character))
                    continue;

                if (character.IsDead)
                    continue;

                if (Vector3.Distance(character.transform.position,
                        _mainShieldTarget.transform.position) > _aoeRadius + 0.5f)
                    continue;

                newTargets.Add(character);
            }

            ApplyAoeDiff(newTargets);
        }
        
        [Server]
        private void ApplyAoeDiff(HashSet<Character> current)
        {
            foreach (var c in current)
            {
                if (_aoeShieldTargets.Add(c))
                {
                    c.Health.Shields.Add(_shield);
                }
            }

            var toRemove = new List<Character>();

            foreach (var old in _aoeShieldTargets)
            {
                if (!current.Contains(old))
                {
                    if (old != null)
                        old.Health.Shields.Remove(_shield);

                    toRemove.Add(old);
                }
            }

            foreach (var r in toRemove)
                _aoeShieldTargets.Remove(r);
        }

        [Server]
        private void RemoveAoeShields()
        {
            foreach (var character in _aoeShieldTargets)
            {
                if (character != null && _shield != null)
                    character.Health.Shields.Remove(_shield);
            }
            _aoeShieldTargets.Clear();
        }

        private void OnAbsorb(Damage damage, Skill skill)
        {
            float now = Time.time;
            if (now - _lastDamageTime > 1f)
                _damagePerSecond = 0;

            _lastDamageTime = now;
            _damagePerSecond += damage.Value;

            if (_damagePerSecond > 50f)
            {
                BreakAbsorption();
                return;
            }

            _absorbedDamage += damage.Value;

            if (_absorbedDamage >= _shieldValue)
                _shieldBroken = true;

            RpcManaSpendJob(_hero.gameObject, damage.Value);

            if (_shield)
                RpcChangeShieldAlpha(_shield.gameObject);
        }

        private void BreakAbsorption()
        {
            if (!isServer) return;

            if (_isAoeShieldActive)
                TargetRpcStopAoeScan(connectionToClient);

            RemoveAoeShields();

            if (_shield != null)
            {
                _shield.DamageTaken -= OnAbsorb;
                _shield.TryUse(99999);
                NetworkServer.Destroy(_shield.gameObject);
                _shield = null;
            }

            ClearSkillInfo();
            RpcControlMovement(true);
            TryCancel(true);
        }

        [ClientRpc]
        private void RpcControlMovement(bool canMove)
        {
            _hero.Move.SetCanMove(canMove);
            if (!canMove)
                _hero.Move.StopMoveAndAnimationMove();
        }

        [TargetRpc]
        private void RpcManaSpendJob(GameObject target, float count)
        {
            if (target == null) return;
            target.TryGetComponent(out Character character);
            character.TryGetResource(ResourceType.Mana).CmdUse(count);
        }

        [TargetRpc]
        private void RpcAbsorbJob(GameObject target, float damage)
        {
            if (target == null) return;
            target.TryGetComponent(out Character character);
            character.TryGetResource(ResourceType.Mana).CmdAdd(damage * _absorbationMultiplier);
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

        private void OnDestroy()
        {
            if (_shield != null)
            {
                _shield.StopFollowTo();
                NetworkServer.Destroy(_shield.gameObject);
                _shield = null;
            }
        }
    }
}