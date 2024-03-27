using System;
using System.Collections;
using GlobalEvents;
using UnityEngine;

namespace Players.Abilities.Genjalf.Fireworks_Ability
{
    public class ShootFireworks : MonoBehaviour
    {
        [SerializeField] private GameObject _prefabFireworks;
        [SerializeField] private Transform _startPosShoot;
        [SerializeField] private float _mana = 1000f;

        private void Awake()
        {
            StartFireworksEvent.OnStartFireworksEvent.AddListener(StartLostMana);
            StopFireworksEvent.OnStopFireworksEvent.AddListener(StopLostMana);
        }

        private void Update()
        {
            Shoot();
        }

        private void Shoot()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Instantiate(_prefabFireworks, _startPosShoot.position, _startPosShoot.rotation);
            }
        }

        private void StartLostMana()
        {
            StartCoroutine(nameof(LostMana));
        }

        private void StopLostMana()
        {
            StopCoroutine(nameof(LostMana));
        }

        private IEnumerator LostMana()
        {
            while (true)
            {
                _mana -= 3f;
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}