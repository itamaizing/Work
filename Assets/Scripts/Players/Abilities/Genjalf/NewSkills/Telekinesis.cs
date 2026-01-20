using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Gangdollarff
{
    public class Telekinesis : Skill, IGodLightSpell
    {
        [SerializeField] private float _deleyTelekines = 0.5f;
        [SerializeField] private float _amountOfLift = 1.5f;
        [SerializeField] private DecalProjector _radiusEnemy;
        
        private Vector3 _point = Vector3.zero;
        private Vector3 _originalGroundPosition;
        
        private float _tempCastDeley = 1;
        private float _clickRadius = 0.5f;
        private float _secPerMeter = 0.4f;

        protected override int AnimTriggerCastDelay => 0;

        protected override int AnimTriggerCast => Animator.StringToHash("Telekinesis");

        protected override bool IsCanCast => CheckCanCast();

        public bool IsEnabled { get; private set; }

        private bool CheckCanCast()
        {
            return Vector3.Distance(GetTargetCharacter().transform.position, transform.position) <= Radius;
        }

        private void OnEnable()
        {
            Canceled += ForceDropTarget;
        }

        private void OnDisable()
        {
            Canceled -= ForceDropTarget;
        }

        public void AnimCastTelekinesis()
        {
            AnimStartCastCoroutine();
        }

        public void AnimTelekinesisEnd()
        {
            AnimCastEnded();
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

        public override void LoadTargetData(TargetInfo targetInfo)
        {
            SetTarget((ITargetable)(Character)targetInfo.GetTargets()[0]);
        }

        protected override IEnumerator CastJob()
        {
            DisableMove();
            float hangStartTime = Time.time;
            var targetChar = GetTargetCharacter();
            var targetGO = targetChar.gameObject;
            Vector3 currentPos = targetChar.transform.position;
            Vector3 liftPos = new Vector3(currentPos.x, currentPos.y + _amountOfLift, currentPos.z);
            float liftTime = _deleyTelekines;
            _originalGroundPosition = new Vector3(currentPos.x, currentPos.y, currentPos.z);

            CmdMoveTaget(targetGO, liftPos, liftTime);
            yield return new WaitForSeconds(liftTime);

            _point = Vector3.zero;
            bool destinationSet = false;

            while (Time.time - hangStartTime < (_castDuration - liftTime))
            {
                if (Input.GetMouseButtonDown(0))
                {
                    _point = GetMousePoint();
                    if (_point != Vector3.zero &&
                        Vector3.Distance(_point, transform.position) <= Radius &&
                        Vector3.Distance(_point, targetChar.transform.position) <= Radius)
                    {
                        destinationSet = true;
                        break;
                    }
                }
                yield return null;
            }

            if (destinationSet)
            {
                Vector3 aboveSecond = new Vector3(_point.x, _point.y + _amountOfLift, _point.z);
                float distance = Vector3.Distance(targetChar.transform.position, aboveSecond);
                float moveTime = distance * _secPerMeter;

                CmdMoveTaget(targetGO, aboveSecond, moveTime);
                yield return new WaitForSeconds(moveTime);

                CmdMoveTaget(targetGO, new Vector3(_point.x, currentPos.y, _point.z), liftTime);
                yield return new WaitForSeconds(liftTime);
            }
            else
            {
                Vector3 dropPos = new Vector3(targetChar.transform.position.x, currentPos.y, targetChar.transform.position.z);
                CmdMoveTaget(targetGO, dropPos, liftTime);
                yield return new WaitForSeconds(liftTime);
            }

            float elapsedTime = Time.time - hangStartTime;
            if (elapsedTime < _castDuration)
            {
                yield return new WaitForSeconds(_castDuration - elapsedTime);
            }
            EnableMove();
        }

        private void ForceDropTarget()
        {
            StopCoroutine(CastJob());
            Character _targetChar = GetTargetCharacter();
            if (_targetChar != null)
            {
                Debug.LogError("This");
                Vector3 currentPos = _targetChar.transform.position;
                Vector3 dropPos = new Vector3(currentPos.x, _originalGroundPosition.y, currentPos.z);

                CmdForceDrop(_targetChar.gameObject, dropPos, _deleyTelekines);
            }
            EnableMove();
        }
        
        protected override void ClearData()
        {
            ClearTarget();
            ClearTempTarget();
            //_target = null;
            _radiusEnemy.gameObject.SetActive(false);
        }

        protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
        {
            _skillRender.DrawRadius(Radius);
            while (GetTempTargetCharacter() == null)
            {
                if (GetMouseButton)
                {
                    Vector3 clickPoint = GetMousePoint();

                    FindTarget(_clickRadius, clickPoint, canTargetHimself: true);
                }
                yield return null;
            }
            TargetInfo targetInfo = new TargetInfo();
            SetTarget(GetTempTargetCharacter());
            targetInfo.AddTarget(GetTargetCharacter());
            callbackDataSaved(targetInfo);
           
            yield return new WaitForSeconds(0.1f);

            _skillRender.StopDrawRadius();

            _radiusEnemy.gameObject.SetActive(true);
            _radiusEnemy.transform.parent = GetTargetCharacter().transform;
            _radiusEnemy.transform.localPosition = Vector3.zero;
        }

        private void EnableMove()
        {
            Hero.Move.IsMoveBlocked = false;
            Hero.Move.StopLookAt();
        }

        private void DisableMove()
        {
            Hero.Move.IsMoveBlocked = true;
        }

        [Command]
        private void CmdMoveTaget(GameObject target, Vector3 point, float duration)
        {
            var enemyMove = target.GetComponent<MoveComponent>();
            var targetCharacter = target.GetComponent<Character>();

            float moveDuration = duration;

            if (targetCharacter.connectionToClient != null)
                enemyMove.TargetRpcDoMove(point, moveDuration);
            else
                enemyMove.RpcDoMove(point, moveDuration);
        }

        [Command]
        private void CmdForceDrop(GameObject target, Vector3 dropPos, float duration)
        {
            var enemyMove = target.GetComponent<MoveComponent>();
            var targetCharacter = target.GetComponent<Character>();

            if (targetCharacter.connectionToClient != null)
                enemyMove.TargetRpcForceDrop(targetCharacter.connectionToClient, dropPos, duration);
            else
                enemyMove.RpcForceDrop(dropPos, duration);
        }
    }
}