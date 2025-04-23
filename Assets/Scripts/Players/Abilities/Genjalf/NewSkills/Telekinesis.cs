using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gangdollarff
{
    public class Telekinesis : Skill
    {
        [SerializeField] private float _deleyTelekines = 0.5f;
        [SerializeField] private float _amountOfLift = 1.5f;

        private Character _target;
        private Vector3 _point = Vector3.zero;

        protected override int AnimTriggerCastDelay => 0;

        protected override int AnimTriggerCast => Animator.StringToHash("Telekinesis");

        protected override bool IsCanCast => CheckCanCast();

        private bool CheckCanCast()
        {
            return Vector3.Distance(_point, transform.position) <= Radius &&
                   Vector3.Distance(_target.transform.position, transform.position) <= Radius;
        }

        public void AnimCastTelekinesis()
        {
            AnimStartCastCoroutine();
        }

        public void AnimTelekinesisEnd()
        {
            AnimCastEnded();
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
        }

        protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
        {
            while (_target == null)
            {
                if (GetMouseButton)
                    _target = GetRaycastTarget(true);

                yield return null;
            }
            yield return new WaitForSeconds(0.1f);

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
            //enemyMove.DoMove(point, time - _deleyTelekines);
            enemyMove.TargetRpcDoMove(point, time);
        }
    }
}