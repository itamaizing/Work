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

        private Character _tempChar;

        private Vector3 _secondClickPoint;
        
        private float _originalGroundPosition;
        private float _tempCastDeley = 1;
        private float _clickRadius = 0.5f;
        private float _secPerMeter = 0.4f;

        protected override int AnimTriggerCastDelay => 0;

        protected override int AnimTriggerCast => Animator.StringToHash("Telekinesis");

        private bool _isSecondClick;
        private bool _isLifted;

        protected override bool IsCanCast => CheckCanCast();

        public bool IsEnabled { get; private set; }

        private bool CheckCanCast()
        {
            if (Targeting.GetTarget()?.Character != null)
            {
                if (!_isSecondClick && !_isLifted)
                    return Vector3.Distance(Targeting.GetTarget().Transform.position, transform.position) <= AreaInfo.Radius;
                else if(_isLifted)
                    return Vector3.Distance(Targeting.GetTarget().Transform.position, transform.position) <= AreaInfo.Radius + _amountOfLift;
                else
                    return Vector3.Distance(Targeting.GetTarget().Transform.position, _secondClickPoint) <= AreaInfo.Radius;
            }

            return false;
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
            _isLifted = true;
        }

        public void AnimTelekinesisEnd()
        {
            AnimCastEnded();
            _isLifted = false;
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

                _tempCastDeley = Cooldown.CooldownTime;
                Cooldown.CooldownTime = 0;
            }
        }

        public override void LoadTargetData(TargetInfo targetInfo)
        {
            Targeting.SetTarget((ITargetable)(Character)targetInfo.GetTargets()[0]);
        }

        protected override IEnumerator CastJob()
        {
            DisableMove();
            _tempChar = Targeting.GetTarget()?.Character;
            
            if(!_tempChar) yield break;
            _skillRender.SetPrepareCursor();
            Hero.Abilities.SetAbilitiesDisactive(true);
            
            CmdAddState(_tempChar);
            
            var targetGO = _tempChar.gameObject;
            _originalGroundPosition = _tempChar.transform.position.y;
            Vector3 startPos = _tempChar.transform.position;
            Vector3 hoverOffset = new Vector3(0, _amountOfLift, 0);

            float castStartTime = Time.time;

            CmdMoveTaget(targetGO, startPos + hoverOffset, _deleyTelekines);
            yield return new WaitForSeconds(_deleyTelekines);
            
            _radiusEnemy.gameObject.SetActive(true);
            _radiusEnemy.transform.parent = Targeting.GetTarget()?.Transform;
            _radiusEnemy.transform.localPosition = Vector3.zero;

            while (Time.time - castStartTime < _castDuration)
            {
                if (Input.GetMouseButtonDown(0) && !_isSecondClick)
                {
                    _secondClickPoint = Targeting.GetMousePoint();
                    if (_secondClickPoint != Vector3.zero &&
                        Vector3.Distance(_secondClickPoint, _tempChar.transform.position) <= AreaInfo.Radius)
                    {
                        _isLifted = false;
                        _isSecondClick = true;
                        _skillRender.ResetCursor();
                        _radiusEnemy.gameObject.SetActive(false);

                        Vector3 hoverTarget = new Vector3(
                            _secondClickPoint.x,
                            _originalGroundPosition + _amountOfLift,
                            _secondClickPoint.z
                        );

                        float dist = Vector3.Distance(_tempChar.transform.position, hoverTarget);
                        float t = dist * _secPerMeter;

                        CmdMoveTaget(targetGO, hoverTarget, t);
                    }
                }

                yield return null;
            }

            ForceDropTarget();
        }

        private void ForceDropTarget()
        {
            StopCoroutine(CastJob());
            _skillRender.ResetCursor();
            if (_tempChar != null)
            {
                Vector3 currentPos = _tempChar.transform.position;
                Vector3 dropPos = new Vector3(currentPos.x, _originalGroundPosition, currentPos.z);

                CmdForceDrop(_tempChar.gameObject, dropPos, _deleyTelekines);
                CmdRemoveState(_tempChar);
            }

            _tempChar = null;
            Hero.Abilities.SetAbilitiesDisactive(false);
            EnableMove();

            ClearData();
        }
        
        protected override void ClearData()
        {
            Targeting.ClearTarget();
            Targeting.ClearTempTarget();
            //_target = null;
            _radiusEnemy.gameObject.SetActive(false);
            _isSecondClick = false;
            _isLifted = false;
        }

        protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
        {
            _skillRender.DrawRadius(AreaInfo.Radius);
            while (Targeting.GetTempTarget()?.Character == null)
            {
                if (GetMouseButton)
                {
                    Vector3 clickPoint = Targeting.GetMousePoint();

                    Targeting.FindTempTarget(clickPoint, _clickRadius, canTargetSelf: true);
                }
                yield return null;
            }
            TargetInfo targetInfo = new TargetInfo();
            Targeting.SetTarget(Targeting.GetTempTarget()?.Character);
            targetInfo.AddTarget(Targeting.GetTarget()?.Character);
            callbackDataSaved(targetInfo);
           
            yield return new WaitForSeconds(0.1f);

            _skillRender.StopDrawRadius();
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
        
        [Command] private void CmdAddState(Character target) => target.CharacterState.AddState(States.Stun, _castDuration, 0,Hero.gameObject, name);
        [Command] private void CmdRemoveState(Character target) => target.CharacterState.RemoveState(States.Stun);

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
