using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gangdollarff
{
    public class Fisura : Skill
    {
        [SerializeField] private LineRenderer _lineRenderer;
        [SerializeField] private FisuraTile _fisuraPref;
        [SerializeField] private float _fisuraDuration = 6;
        [SerializeField, Range(1, 7)] private int _fisuraMaxLenght;

        private Vector3 _startPoint = Vector3.zero;
        private Vector3 _endPoint = Vector3.zero;
        private FisuraTile _fisuraTail;

        public override string AdditionalDescription =>
            $"Длительность: {AbilityNameBox.ColorOpen}{_fisuraDuration} сек{AbilityNameBox.ColorEnd}";

        protected override int AnimTriggerCastDelay => Animator.StringToHash("FisuraCast");

        protected override int AnimTriggerCast => Animator.StringToHash("Fisura");

        protected override bool IsCanCast => CheckCanCast();

        private bool CheckCanCast()
        {
            return Vector3.Distance(_startPoint, transform.position) <= Radius;
        }

        public void AnimCastFisura()
        {
            AnimStartCastCoroutine();
        }

        public void AnimFisuraEnd()
        {
            AnimCastEnded();
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
            _lineRenderer.transform.parent = null;
            _lineRenderer.positionCount = 2;

            while (_startPoint == Vector3.zero)
            {
                if (GetMouseButton)
                    _startPoint = GetMousePoint();

                yield return null;
            }
            _lineRenderer.SetPosition(0, _startPoint);
            yield return new WaitForSeconds(0.1f);

            while (_endPoint == Vector3.zero)
            {
                if (Input.GetMouseButton(0))
                    _endPoint = GetMousePoint();

                _lineRenderer.SetPosition(1, GetMousePoint());
                yield return null;
            }

            _lineRenderer.positionCount = 0;
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
