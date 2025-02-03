using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gangdollarff
{
    public class Fisura : Skill
    {
        [SerializeField] private FisuraTile _fisuraPref;
        [SerializeField] private float _fisuraDuration = 6;
        [SerializeField, Range(1, 7)] private int _fisuraMaxLenght;

        private Vector3 _startPoint = Vector3.zero;
        private Vector3 _endPoint = Vector3.zero;
        private FisuraTile _fisuraTail;

        protected override int AnimTriggerCastDelay => 0;

        protected override int AnimTriggerCast => 0;

        protected override bool IsCanCast => CheckCanCast();

        private bool CheckCanCast()
        {
            return Vector3.Distance(_startPoint, transform.position) <= Radius;
        }

        protected override IEnumerator CastJob()
        {
            CmdUse(_startPoint, _endPoint);
            yield return null;
        }

        protected override void ClearData()
        {
            _startPoint = Vector3.zero;
            _endPoint = Vector3.zero;
        }

        protected override IEnumerator PrepareJob()
        {
            while (_startPoint == Vector3.zero)
            {
                if (GetMouseButton)
                    _startPoint = GetMousePoint();

                yield return null;
            }
            yield return new WaitForSeconds(0.1f);

            while (_endPoint == Vector3.zero)
            {
                if (Input.GetMouseButton(0))
                    _endPoint = GetMousePoint();

                yield return null;
            }

            yield return null;
        }

        [Command]
        private void CmdUse(Vector3 startPoint, Vector3 endPoint)
        {
            GameObject item = Instantiate(_fisuraPref.gameObject, startPoint, Quaternion.identity);

            SceneManager.MoveGameObjectToScene(item, _hero.NetworkSettings.MyRoom);

            NetworkServer.Spawn(item);

            _fisuraTail = item.GetComponent<FisuraTile>();

            _fisuraTail.SetStartPosition(startPoint);
            _fisuraTail.SetEndPosition(endPoint);

            _fisuraTail.Build();

            StartCoroutine(DurationJob());
        }

        private IEnumerator DurationJob()
        {
            yield return new WaitForSecondsRealtime(_fisuraDuration);
            NetworkServer.Destroy(_fisuraTail.gameObject);
        }
    }
}
