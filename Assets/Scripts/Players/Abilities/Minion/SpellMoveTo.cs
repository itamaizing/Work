using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SpellMoveTo : Skill
{
    [SerializeField] private NavMeshAgent _agent;
    [SerializeField] private float _stopDistance = 0.5f;

    private Vector3 _targetPoint = Vector3.positiveInfinity;
    private Character _target = null;
    private Character _enemyTarget = null;
    private float _currentDamageDeley;
    private bool _isHolding = false;
    private bool _detectingClick = false;
    private float _clickDetectTime = 0;
    private Vector3 _tempPoint;
    private Character _tempTarget;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;
    protected override bool IsCanCast => true;

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0) Targeting.SetTarget(targetInfo.GetTargets()[0]);
        _targetPoint = targetInfo.Points[0];
    }

    protected override IEnumerator CastJob()
    {
        _isHolding = true;
        _agent.SetDestination(_targetPoint);

        while (true)
        {
            if (_target != null)
                _targetPoint = _target.transform.position;

            _currentDamageDeley = 0;

            if (_isHolding && Input.GetMouseButton(0))
            {
                Character newTarget = Targeting.GetTarget()?.Character;
                if (newTarget != null)
                {
                    _target = newTarget;
                    _targetPoint = _target.transform.position;
                }
                else
                {
                    _target = null;
                    _targetPoint = Targeting.GetMousePoint();
                }

                _agent.SetDestination(_targetPoint);
            }

            if (_isHolding && !Input.GetMouseButton(0))
            {
                _isHolding = false;
            }

            if (!_isHolding && !_agent.pathPending && _agent.remainingDistance <= _stopDistance)
            {
                break;
            }

            yield return null;
        }

        ClearData();
    }

    protected override void ClearData()
    {
        _agent.SetDestination(transform.position);
        _targetPoint = Vector3.positiveInfinity;
        _target = null;
        _isHolding = false;
        _detectingClick = false;
        _currentDamageDeley = 0;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
    {
        TargetInfo targetInfo = new TargetInfo();

        while (!Input.GetMouseButtonDown(0))
            yield return null;

        Character initialTarget = Targeting.GetTarget()?.Character;
        Vector3 initialPoint = Targeting.GetMousePoint();

        if (initialTarget != null)
        {
            targetInfo.AddTarget(initialTarget);
            initialPoint = initialTarget.transform.position;
        }

        targetInfo.Points.Add(initialPoint);
        targetDataSavedCallback(targetInfo);
    }
}
