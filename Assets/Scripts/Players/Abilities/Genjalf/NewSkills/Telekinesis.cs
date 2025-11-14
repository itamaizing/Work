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

        private Character _target;
        private Vector3 _point = Vector3.zero;
        private float _tempCastDeley = 1;

        protected override int AnimTriggerCastDelay => 0;

        protected override int AnimTriggerCast => Animator.StringToHash("Telekinesis");

        protected override bool IsCanCast => CheckCanCast();

        public bool IsEnabled { get; private set; }

        private bool CheckCanCast()
        {
            return Vector3.Distance(_point, transform.position) <= Radius &&
                   Vector3.Distance(_target.transform.position, _point) <= Radius;
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
            _point = targetInfo.Points[0];
            _target = (Character)targetInfo.Targets[0];
        }

        protected override IEnumerator CastJob()
        {
            DisableMove();

            CmdMoveTaget(_target.gameObject, new Vector3(_target.transform.position.x, _target.transform.position.y + _amountOfLift, _target.transform.position.z), _deleyTelekines);
            yield return new WaitForSeconds(_deleyTelekines);
            CmdMoveTaget(_target.gameObject, _point, CastStreamDuration - _deleyTelekines);
        }
        protected override void ClearData()
        {
            EnableMove();
            _target = null;
            _point = Vector3.zero;
            _radiusEnemy.gameObject.SetActive(false);
        }

        protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
        {
            while (_target == null)
            {
                if (GetMouseButton)
                    if(GetTarget().character != null)
                    _target = GetTarget().character;

                yield return null;
            }
            yield return new WaitForSeconds(0.1f);

            _radiusEnemy.gameObject.SetActive(true);
            _radiusEnemy.transform.parent = _target.transform;
            _radiusEnemy.transform.localPosition = Vector3.zero;

            while (_point == Vector3.zero)
            {
                if (Input.GetMouseButton(0))
                    _point = GetMousePoint();

                yield return null;
            }
            TargetInfo targetInfo = new TargetInfo();
            targetInfo.Targets.Add( _target );
            targetInfo.Points.Add( _point );
            callbackDataSaved(targetInfo);
            _radiusEnemy.gameObject.SetActive(false);
        }

        private void EnableMove()
        {
            Hero.Move.IsMoveBlocked = false;
        }

        private void DisableMove()
        {
            Hero.Move.IsMoveBlocked = true;
        }

        [Command]
        private void CmdMoveTaget(GameObject target, Vector3 point, float time)
        {
            var enemyMove = target.GetComponent<MoveComponent>();
            var targetCharacter = target.GetComponent<Character>();
			//enemyMove.DoMove(point, time - _deleyTelekines);
			if (targetCharacter.connectionToClient != null) enemyMove.TargetRpcDoMove(point, 0.05f);
			else enemyMove.RpcDoMove(point, 0.05f);
			//enemyMove.TargetRpcDoMove(point, time);
        }
    }
}