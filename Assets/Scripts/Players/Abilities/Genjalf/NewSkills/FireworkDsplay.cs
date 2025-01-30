using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gangdollarff
{
    public class FireworkDsplay : Skill
    {
        [SerializeField] private Firework _firework;

        private Vector3 _targetPoint = Vector3.positiveInfinity;
        private Character _target;

        protected override int AnimTriggerCastDelay => 0;

        protected override int AnimTriggerCast => 0;

        protected override bool IsCanCast { get => CheckCanCast(); }

        private bool CheckCanCast()
        {
            if (_target == null)
                return Vector3.Distance(_targetPoint, transform.position) <= Radius;

            return Vector3.Distance(_targetPoint, transform.position) <= Radius ||
                   Vector3.Distance(_target.transform.position, transform.position) <= Radius;
        }

        protected override IEnumerator CastJob()
        {
            float time = 0;
            _firework.gameObject.SetActive(true);

            while (time < CastStreamDuration)
            {
                yield return new WaitForSeconds(_manaCostRate);

                int count = 0;

                foreach (var item in _firework.Damageables)
                {
                    if (((1 << item.gameObject.layer) & TargetsLayers) != 0)
                    {
                        if (item.TryGetComponent<IDamageable>(out IDamageable enemy) && count < 3)
                        {

                            count++;
                            Damage damage = new Damage
                            {
                                Value = Buff.Damage.GetBuffedValue(Damage),
                                Type = DamageType,
                                PhysicAttackType = AttackRangeType,
                            };

                            CmdApplyDamage(damage, item.gameObject);
                        }
                    }
                }
                time += _manaCostRate;
                yield return null;
            }
            ClearData();
        }

        protected override void ClearData()
        {
            _firework.gameObject.SetActive(false);
            _target = null;
            _targetPoint = Vector3.positiveInfinity;
        }

        protected override IEnumerator PrepareJob()
        {
            while (float.IsPositiveInfinity(_targetPoint.x) && _target == null)
            {
                if (GetMouseButton)
                {
                    _target = GetTarget().character;
                    _targetPoint = GetTarget().Position;

                    _target = GetRaycastTarget();
                    _targetPoint = GetMousePoint();
                }
                yield return null;
            }
            yield return null;
        }
    }
}

