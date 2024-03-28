using GlobalEvents;
using UnityEngine;

namespace Players.Abilities.Genjalf.Fireworks_Ability.Version_In_Genjalf_Fireworks
{
    public class ShootV2 : MonoBehaviour
    {
        [SerializeField] private float _mana = 1000f;
        [SerializeField] private GameObject _fireWorks;

        private void Awake()
        {
            StartFireworksEvent.OnStartFireworksEvent.AddListener(_fireWorks.GetComponent<Fireworks>().StartTimeToEndFireworks);

                StopFireworksEvent.OnStopFireworksEvent.AddListener(DisableFireworks);
        }

        public float Mana
        {
            get => _mana;
            set => _mana = value;
        }

        private void Update()
        {
            Shoot();
        }

        private void Shoot()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (_mana > 0)
                {
                    _fireWorks.SetActive(true);
                    StartFireworksEvent.SendStartFireworksEvent();
                }
            }
        }

        private void DisableFireworks()
        {
            _fireWorks.GetComponent<Fireworks>().StopTimeToEndFireworks();
            _fireWorks.SetActive(false);
        }
    }
}