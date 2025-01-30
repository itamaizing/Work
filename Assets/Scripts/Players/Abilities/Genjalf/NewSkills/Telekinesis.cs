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

        protected override int AnimTriggerCast => 0;

        protected override bool IsCanCast => CheckCanCast();

        private bool CheckCanCast()
        {
            return Vector3.Distance(_point, transform.position) <= Radius &&
                   Vector3.Distance(_target.transform.position, transform.position) <= Radius;
        }

        protected override IEnumerator CastJob()
        {
            CmdMoveTaget(_target.gameObject, new Vector3(_target.transform.position.x, _target.transform.position.y + _amountOfLift, _target.transform.position.z), _deleyTelekines);
            yield return new WaitForSeconds(_deleyTelekines);
            CmdMoveTaget(_target.gameObject, _point, CastStreamDuration - _deleyTelekines);
        }
        protected override void ClearData()
        {
            _target = null;
            _point = Vector3.zero;
        }

        protected override IEnumerator PrepareJob()
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