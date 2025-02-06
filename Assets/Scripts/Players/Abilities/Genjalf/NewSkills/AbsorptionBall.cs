using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gangdollarff
{
    public class AbsorptionBall : Skill
    {
        [SerializeField] private Shield _shieldPref;
        [SerializeField] private float _shieldValue = 40;
        [SerializeField] private float _shieldDuration = 2;

        private Shield _shield;

        protected override int AnimTriggerCastDelay => 0;

        protected override int AnimTriggerCast => 0;

        protected override bool IsCanCast => true;

        protected override IEnumerator CastJob()
        {
            CmdAddShield();
            yield return null;
        }

        protected override void ClearData()
        {
            
        }

        protected override IEnumerator PrepareJob()
        {
            yield return null;
        }

        [Command]
        private void CmdAddShield()
        {
            if(_shield != null)
            {
                NetworkServer.Destroy(_shield.gameObject);
            }

            var shield = Instantiate(_shieldPref, transform.position, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(shield.gameObject, _hero.NetworkSettings.MyRoom);
            shield.Initialize(_shieldValue, DamageType.Both);
            NetworkServer.Spawn(shield.gameObject);
            _shield = shield;
            Hero.Health.Shields.Add(shield);
            StartCoroutine(ShieldJob());

            ClientRpcShieldFollow(_shield.gameObject);
        }

        [ClientRpc]
        private void ClientRpcShieldFollow(GameObject shield)
        {
            shield.GetComponent<Shield>().FollowTo(transform);
        }

        private IEnumerator ShieldJob()
        {
            yield return new WaitForSecondsRealtime(_shieldDuration);

            if (_shield != null)
            {
                _shield.TryUse(99999);
                NetworkServer.Destroy(_shield.gameObject);
            }
        }
    }
}