using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gangdollarff
{
    public class Fisura : Skill, IGodLightSpell
    {
        [SerializeField] private LineRenderer _lineRenderer;
        [SerializeField] private FisuraTile _fisuraPref;
        [SerializeField] private float _fisuraDuration = 6;
        [SerializeField, Range(1, 7)] private int _fisuraMaxLenght;

        private Vector3 _startPoint = Vector3.zero;
        private Vector3 _endPoint = Vector3.zero;
        private FisuraTile _fisuraTail;
        private float _tempCastDeley = 1;
        private float _longPressThreshold = 0.25f;

        public override string AdditionalDescription =>
            $"Длительность: {AbilityNameBox.ColorOpen}{_fisuraDuration} сек{AbilityNameBox.ColorEnd}";

        protected override int AnimTriggerCastDelay => Animator.StringToHash("FisuraCast");

        protected override int AnimTriggerCast => Animator.StringToHash("Fisura");

        protected override bool IsCanCast => CheckCanCast();

        public bool IsEnabled { get; set; }

        private bool CheckCanCast()
        {
            return Vector3.Distance(_startPoint, transform.position) <= AreaInfo.Radius;
        }

        public void AnimCastFisura()
        {
            AnimStartCastCoroutine();
        }

        public void AnimFisuraEnd()
        {
            AnimCastEnded();
        }

        public override void LoadTargetData(TargetInfo targetInfo)
        {
            _startPoint = targetInfo.Points[0];
            _endPoint = targetInfo.Points[1];
        }

        public void ChangeMode()
        {
            if (IsEnabled)
            {
                IsEnabled = false;

                _castDeley = _tempCastDeley;
            }
            else
            {
                IsEnabled = true;

                _tempCastDeley = _cooldownTime;
                _cooldownTime = 0;
            }
        }

        protected override IEnumerator CastJob()
        {
            CmdUse(_startPoint, _endPoint);
            yield return null;
        }

        protected override void ClearData()
        {
            _lineRenderer.positionCount = 0;
            _startPoint = Vector3.zero;
            _endPoint = Vector3.zero;
        }

        protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
        {
            TargetInfo targetInfo = new TargetInfo();
            Vector3 firstPoint = Vector3.zero;

            while (!Input.GetMouseButtonDown(0))
                yield return null;

            float downTime = Time.time;
            firstPoint = Targeting.GetMousePoint();
            targetInfo.Points.Add(firstPoint);

            while (!Input.GetMouseButtonUp(0))
            {
                if (Time.time - downTime > _longPressThreshold)
                {
                    Vector3 holdPoint = Targeting.GetMousePoint();
                    if (targetInfo.Points.Count == 1)
                        targetInfo.Points.Add(holdPoint);
                    else
                        targetInfo.Points[1] = holdPoint;
                }
                yield return null;
            }

            bool longClick = (Time.time - downTime) > _longPressThreshold;

            if (longClick)
            {
                Vector3 secondPointOnUp = Targeting.GetMousePoint();
                if (targetInfo.Points.Count == 1)
                    targetInfo.Points.Add(secondPointOnUp);
                else
                    targetInfo.Points[1] = secondPointOnUp;
            }
            else
            {
                while (!Input.GetMouseButtonDown(0))
                    yield return null;

                Vector3 secondPoint = Targeting.GetMousePoint();

                while (!Input.GetMouseButtonUp(0))
                    yield return null;

                targetInfo.Points.Add(secondPoint);
            }

            callbackDataSaved.Invoke(targetInfo);
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
