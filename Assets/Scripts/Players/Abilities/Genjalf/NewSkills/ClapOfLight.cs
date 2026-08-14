using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Gangdollarff
{
    public class ClapOfLight : Skill
    {
        [SerializeField] private ParticleSystem _particle;
        [SerializeField] private float _pushRange = 1;
        [SerializeField] private float _pushDuration = 0.33f;

        public bool IsBaffed = false;

        public bool _isBlockingActive = false;

        private const float RepulsionDuration = 2f;
        private const float RepulsionCheckInterval = 0.1f;
        private const float RepulsionRange = 2f;

        private Transform _heroTransformClient;

        public override string AdditionalDescription =>
            $"Расстояние толчка: {AbilityNameBox.ColorOpen}{_pushRange}{AbilityNameBox.ColorEnd}";

        protected override int AnimTriggerCastDelay => 0;

        protected override int AnimTriggerCast => 0;

        protected override bool IsCanCast => true;

        public override void LoadTargetData(TargetInfo targetInfo)
        {

        }
        
        public void EnableBlockingClapTalent(bool value)
        {
            if(value == _isBlockingActive) return;

            _isBlockingActive = value;
        }

        protected override IEnumerator CastJob()
        {
            var colliders = Physics.OverlapSphere(transform.position, AreaInfo.Radius, Targeting.Layer);

            CmdSetActiveParticle(true);

            foreach (var item in colliders)
            {
                if (item.TryGetComponent(out Character enemy))
                {
                    if (IsBaffed)
                        Cooldown.CooldownTime = Cooldown.CooldownTime - 2;

                    float casterRadius = ((CapsuleCollider)_hero.Collider).radius;
                    float enemyRadius = ((CapsuleCollider)enemy.Collider).radius;

                    float centerDist = Vector3.Distance(transform.position, enemy.transform.position);
                    float edgeDist = Mathf.Max(centerDist - (casterRadius + enemyRadius), 0f);
                    float damageMul = Mathf.Clamp01(1f - edgeDist / AreaInfo.Radius);

                    Damage scaledDamage = new Damage
                    {
                        Value = Buff.Damage.GetBuffedValue(Damage) * damageMul,
                        Type = Info.DamageType,
                        PhysicAttackType = Info.AttackRangeType,
                    };

                    CmdApplyDamage(scaledDamage, enemy.gameObject);

                    float distToPush = _pushRange - edgeDist;
                    if (distToPush > 0f)
                    {
                        Vector3 dir = (enemy.transform.position - transform.position).normalized;
                        Vector3 pointForPush = enemy.transform.position + dir * distToPush;
                        CmdMoveTaget(enemy.gameObject, pointForPush, _pushDuration);
                    }
                }
            }

            if (_isBlockingActive)
                CmdStartRepulsion();

            yield return null;
        }

        [Command]
        private void CmdStartRepulsion()
        {
            TargetRpcStartRepulsionScan(connectionToClient, _hero.gameObject);
        }

        [TargetRpc]
        private void TargetRpcStartRepulsionScan(NetworkConnection conn, GameObject heroGo)
        {
            _heroTransformClient = heroGo != null ? heroGo.transform : null;
            StartCoroutine(ClientRepulsionScanJob());
        }

        private IEnumerator ClientRepulsionScanJob()
        {
            float elapsed = 0f;

            while (elapsed < RepulsionDuration && _heroTransformClient != null)
            {
                var hits = Physics.OverlapSphere(_heroTransformClient.position, RepulsionRange, Targeting.Layer);

                List<uint> netIds = new();

                foreach (var col in hits)
                {
                    if (col.gameObject == _heroTransformClient.gameObject)
                        continue;

                    if (col.TryGetComponent<Character>(out var character))
                    {
                        if (!character.IsDead && character.gameObject.layer == LayerMask.NameToLayer("Enemy"))
                            netIds.Add(character.netId);
                    }
                }

                if (netIds.Count > 0)
                    CmdApplyRepulsion(netIds);

                yield return new WaitForSeconds(RepulsionCheckInterval);
                elapsed += RepulsionCheckInterval;
            }

            _heroTransformClient = null;
        }

        [Command]
        private void CmdApplyRepulsion(List<uint> netIds)
        {
            if (_hero == null) return;

            foreach (var id in netIds)
            {
                if (!NetworkServer.spawned.TryGetValue(id, out var identity))
                    continue;

                if (!identity.TryGetComponent<Character>(out var character))
                    continue;

                if (character.IsDead)
                    continue;

                float dist = Vector3.Distance(character.transform.position, _hero.transform.position);

                if (dist > RepulsionRange + 0.5f)
                    continue;

                float distToPush = RepulsionRange - dist;
                if (distToPush <= 0f)
                    continue;

                Vector3 dir = (character.transform.position - _hero.transform.position).normalized;
                if (dir == Vector3.zero)
                    dir = UnityEngine.Random.insideUnitSphere.normalized;

                Vector3 pushTarget = character.transform.position + dir * distToPush;

                var enemyMove = character.GetComponent<MoveComponent>();
                enemyMove?.RpcDoPush(pushTarget, RepulsionCheckInterval);
            }
        }

        protected override void ClearData()
        {
        }

        protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
        {
            yield return null;
        }

        [Command]
        private void CmdMoveTaget(GameObject target, Vector3 point, float time)
        {
            var enemyMove = target.GetComponent<MoveComponent>();
            enemyMove.RpcDoPush(point, time);
        }

        [Command]
        private void CmdSetActiveParticle(bool status)
        {
            ClientRpcSetActiveParticle(status);
        }

        [ClientRpc]
        private void ClientRpcSetActiveParticle(bool status)
        {
            _particle.gameObject.SetActive(status);
        }
    }
}
