using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Gangdollarff
{
    public class Telekinesis : Skill
    {
        [SerializeField] private float _deleyTelekines = 0.5f;
        [SerializeField] private float _amountOfLift = 1.5f;
        [SerializeField] private DecalProjector _radiusEnemy;
        [SerializeField] private float _secondClickWindow = 1f;

        private Character _tempChar;
        private Vector3 _secondClickPoint;
        private float _originalGroundPosition;
        private float _tempCastDeley = 1;
        private float _clickRadius = 0.5f;
        private float _secPerMeter = 0.4f;

        protected override int AnimTriggerCastDelay => 0;

        protected override int AnimTriggerCast => Animator.StringToHash("TelekinesSkill");

        private bool _isLifted;

        #region Talents
        private bool _isApplyDamageTalent;
        #endregion

        protected override bool IsCanCast => CheckCanCast();

        public void IsApplyDamageTalent(bool value)
        {
            if (_isApplyDamageTalent == value) return;
            _isApplyDamageTalent = value;
        }
        
        private bool CheckCanCast()
        {
            if (Targeting.GetTarget()?.Character == null) return false;

            if (!CheckResourcesOnSkill()) return false;
            
            if (!_isLifted)
                return Vector3.Distance(Targeting.GetTarget().Transform.position, transform.position) <=
                       AreaInfo.Radius;

            return Vector3.Distance(Targeting.GetTarget().Transform.position, transform.position) <=
                   AreaInfo.Radius + _amountOfLift;
        }

        private void OnEnable() => Canceled += ForceDropTarget;
        private void OnDisable() => Canceled -= ForceDropTarget;

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

        public override void LoadTargetData(TargetInfo targetInfo)
        {
            Targeting.SetTarget((ITargetable)(Character)targetInfo.GetTargets()[0]);

            if (targetInfo.Points.Count > 0)
                _secondClickPoint = (Vector3)targetInfo.Points[0];
            else
                _secondClickPoint = Vector3.zero;
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

            Targeting.SetTarget(Targeting.GetTempTarget()?.Character);
            _skillRender.StopDrawRadius();

            while (GetMouseButton)
                yield return null;

            _radiusEnemy.transform.SetParent(Targeting.GetTarget().Transform);
            _radiusEnemy.transform.localPosition = Vector3.zero;
            _radiusEnemy.gameObject.SetActive(true);

            Vector3 destination = Vector3.zero;

            while (true)
            {
                if (GetMouseButton)
                {
                    Vector3 clickPoint = Targeting.GetMousePoint();
                    if (clickPoint != Vector3.zero)
                    {
                        var targetTransform = Targeting.GetTarget().Transform;
                        if (Vector3.Distance(clickPoint, targetTransform.position) > AreaInfo.Radius)
                            clickPoint = Targeting.ClampToRadius(targetTransform.position, clickPoint, AreaInfo.Radius);

                        destination = clickPoint;
                        break;
                    }
                }
                yield return null;
            }

            _radiusEnemy.gameObject.SetActive(false);
            _radiusEnemy.transform.SetParent(null);

            TargetInfo targetInfo = new TargetInfo();
            targetInfo.AddTarget(Targeting.GetTarget()?.Character);

            if (destination != Vector3.zero)
                targetInfo.Points.Add(destination);

            callbackDataSaved(targetInfo);

            yield return new WaitForSeconds(0.1f);
        }

        protected override IEnumerator CastJob()
        {
            DisableMove();
            _tempChar = Targeting.GetTarget()?.Character;

            if (!_tempChar) yield break;

            _skillRender.SetPrepareCursor();
            Hero.Abilities.SetAbilitiesDisactive(true);
            CmdAddState(_tempChar);

            var targetGO = _tempChar.gameObject;
            _originalGroundPosition = _tempChar.transform.position.y;
            Vector3 startPos = _tempChar.transform.position;

            float castEndTime = Time.time + _castDuration;

            CmdMoveTaget(targetGO, startPos + new Vector3(0, _amountOfLift, 0), _deleyTelekines);
            yield return new WaitForSeconds(_deleyTelekines);
            
            if(_isApplyDamageTalent)
                CmdApplyPercentDamage(_tempChar.gameObject);

            if (_secondClickPoint != Vector3.zero && Time.time < castEndTime)
            {
                Vector3 hoverTarget = new Vector3(
                    _secondClickPoint.x,
                    _originalGroundPosition + _amountOfLift,
                    _secondClickPoint.z
                );

                float dist = Vector3.Distance(_tempChar.transform.position, hoverTarget);
                float travelTime = dist * _secPerMeter;

                bool destinationInRange = Vector3.Distance(
                    new Vector3(_secondClickPoint.x, _tempChar.transform.position.y, _secondClickPoint.z),
                    _tempChar.transform.position
                ) <= AreaInfo.Radius;

                if (destinationInRange)
                {
                    CmdMoveTaget(targetGO, hoverTarget, travelTime);
                }
            }
            while (Time.time < castEndTime)
            {
                yield return null;
            }

            ForceDropTarget();
        }
        
        protected override bool CheckResourcesOnSkill()
        {
            foreach (var cost in _skillEnergyCosts)
            {
                if (!_hero.Resources.TryGetValue(cost.type, out var resource))
                    return false;
                if (resource.CurrentValue < Buff.ManaCost.GetBuffedValue(cost.value))
                    return false;
            }

            return Cost.EnoughResources();
        }

        private void ForceDropTarget()
        {
            _skillRender.ResetCursor();

            if (_tempChar != null)
            {
                Vector3 currentPos = _tempChar.transform.position;
                Vector3 dropPos = new Vector3(currentPos.x, _originalGroundPosition, currentPos.z);

                CmdForceDrop(_tempChar.gameObject, dropPos, _deleyTelekines);
                CmdRemoveState(_tempChar);
            }

            _tempChar = null;
            _isLifted = false;
            Hero.Abilities.SetAbilitiesDisactive(false);
            EnableMove();
            ClearData();
        }

        protected override void ClearData()
        {
            Targeting.ClearTarget();
            Targeting.ClearTempTarget();
            _radiusEnemy.gameObject.SetActive(false);
            _secondClickPoint = Vector3.zero;
            _isLifted = false;
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
        private void CmdAddState(Character target) =>
            target.CharacterState.AddState(States.Stun, _castDuration, 0, Hero.gameObject, name);

        [Command]
        private void CmdRemoveState(Character target) => target.CharacterState.RemoveState(States.Stun);

        [Command]
        private void CmdMoveTaget(GameObject target, Vector3 point, float duration)
        {
            var enemyMove = target.GetComponent<MoveComponent>();
            var targetCharacter = target.GetComponent<Character>();

            if (targetCharacter.connectionToClient != null)
                enemyMove.TargetRpcDoMove(point, duration);
            else
                enemyMove.RpcDoMove(point, duration);
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
        
        [Command]
        private void CmdApplyPercentDamage(GameObject target)
        {
            var health = target.GetComponent<Health>();
            if (health == null) return;

            Damage dmg = new Damage();
            dmg.Value = health.CurrentValue * 0.05f;
            ApplyDamage(dmg, target);
        }
    }
}
