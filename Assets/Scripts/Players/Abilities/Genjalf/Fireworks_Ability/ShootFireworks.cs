using UnityEngine;

namespace Players.Abilities.Genjalf.Fireworks_Ability
{
    public class ShootFireworks:MonoBehaviour
    {
        [SerializeField] private GameObject _prefabFireworks;
        [SerializeField] private Transform _startPosShoot;

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
    }
}