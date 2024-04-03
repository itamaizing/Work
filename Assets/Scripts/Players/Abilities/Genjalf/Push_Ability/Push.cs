using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Players.Abilities.Genjalf.Push_Ability
{
    public class Push : MonoBehaviour
    {
        [SerializeField] private KeyCode _activationButton = KeyCode.Alpha3;
        [Header("Abilities panel")]
        [SerializeField] private GameObject _iconAbility;
        [SerializeField] private Toggle _toggleAbility;
        [SerializeField] private GameObject _abilitiesPanel;
        [Header("Ability settings")]
        [SerializeField] private ParticleSystem _castPrefab; // затычка для визуализации
        [SerializeField] private float _manaCost = 0f;
        [SerializeField] private float _radius = 4f;
        [SerializeField] private float _pushDistance = 4f;
        [SerializeField] private float _duration = 0.5f;

        private Coroutine _pushJob;
        private Dictionary<GameObject, Vector2> _enemies = new Dictionary<GameObject, Vector2>();
        private ManaPlayer _mana;

        private void Start()
        {
            _mana = GetComponentInParent<ManaPlayer>();
        }

        private void Update()
        {
            TryActivatedAbility();
        }

        private void TryActivatedAbility()
        {
            if ((_toggleAbility.gameObject.activeSelf && Input.GetKeyDown(_activationButton) &&
                transform.parent.GetComponent<PlayerMove>().IsSelect && _toggleAbility.enabled &&
                _mana.Mana > _manaCost) == false)
                return;

            _mana.UseMana(_manaCost);
            PlayCost(); // затычка для визуализации

            RaycastHit2D[] hits = Physics2D.CircleCastAll(transform.position, _radius, Vector2.zero);

            if (_pushJob != null)
                StopCoroutine(PushCoroutine());

            _enemies.Clear();

            foreach (var item in hits)
            {
                if (item.transform.CompareTag("Enemies"))
                {
                    Vector3 dir = (item.transform.position - transform.position).normalized * _pushDistance;
                    dir += item.transform.position;

                    _enemies.Add(item.transform.gameObject, dir);
                }
            }
            _pushJob = StartCoroutine(PushCoroutine());
        }

        private void PlayCost() // затычка для визуализации
        {
            var particle = Instantiate(_castPrefab, transform.position, Quaternion.identity, null);
            ParticleSystem.ShapeModule shape = particle.shape;
            shape.radius = _radius;
        }

        private IEnumerator PushCoroutine()
        {
            float time = 0;

            while(_duration > time)
            {
                foreach (var item in _enemies)
                {
                    item.Key.transform.position = Vector2.MoveTowards(item.Key.transform.position, item.Value, (_pushDistance * Time.deltaTime) / _duration);
                }
                time += Time.deltaTime;

                yield return null;
            }
        }
    }
}


