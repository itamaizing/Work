using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Players.Abilities.Genjalf.Push_Ability
{
    public class CottonLight : Ability
    {
        [Header("Ability settings")]
        [SerializeField] private ParticleSystem _castPrefab; // затычка для визуализации
        [SerializeField] private float _damage = 10f;
        [SerializeField] private BlindDebuff _blindPref;
        [SerializeField] private float _blindDuration = 1f;
        [SerializeField] private float _pushDistance;
        [SerializeField] private float _pushDuration;
        [SerializeField] private float _pushSpeed;

        private Coroutine _pushJob;
        private Dictionary<GameObject, Vector2> _enemies = new Dictionary<GameObject, Vector2>();

        private void OnValidate()
        {
            if (_pushSpeed > 0)
            {
                _pushDuration = _pushDistance / _pushSpeed;
            }
            if(_pushDuration > 0)
            {
                _pushSpeed = _pushDistance / _pushDuration;
            }
        }

        protected override void Cast()
        {
            PlayCast(); // затычка для визуализации
            PayCost();

            RaycastHit2D[] hits = Physics2D.CircleCastAll(transform.position, Radius, Vector2.zero);

            if (_pushJob != null)
                StopCoroutine(PushCoroutine());

            _enemies.Clear();

            foreach (var item in hits)
            {
                if (item.transform == transform.parent)
                    continue;

                if (item.transform.CompareTag("Enemies"))
                {
                    Vector3 dir = (item.transform.position - transform.position).normalized * _pushDistance;
                    dir += item.transform.position;

                    _enemies.Add(item.transform.gameObject, dir);
                    item.transform.GetComponent<HealthComponent>().TryTakeDamage(_damage, DamageType.Magical, AttackRangeType.RangeAttack);

                    ApplyDebuffOnTarget(item.transform.gameObject);
                }
            }
            _pushJob = StartCoroutine(PushCoroutine());
        }

        protected override void Cancel()
        {
            
        }

        private void PlayCast() // затычка для визуализации
        {
            var particle = Instantiate(_castPrefab, transform.position, Quaternion.identity, null);
            ParticleSystem.ShapeModule shape = particle.shape;
            shape.radius = Radius;
        }

        private void ApplyDebuffOnTarget(GameObject target)
        {
            Instantiate(_blindPref, target.transform).Init(target.transform.gameObject, _blindDuration);
        }

        private IEnumerator PushCoroutine()
        {
            float time = 0;

            while(_pushDuration > time)
            {
                foreach (var item in _enemies)
                {
                    item.Key.transform.position = Vector2.MoveTowards(item.Key.transform.position, item.Value, (_pushDistance * Time.deltaTime) / _pushDuration);
                }
                time += Time.deltaTime;

                yield return null;
            }
        }
    }
}


