using System;
using System.Collections;
using UnityEngine;

public abstract class CloseCombatSkill : Skill
{
	//TEST CLASS FOR OVERRIDE PREPARE
	private Vector3 _targetPoint = Vector3.positiveInfinity;
	private Character _target;

    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
    {
		//Buff.CastSpeed.IncreasePercentage(_animSpeed);

		while (float.IsPositiveInfinity(_targetPoint.x) && _target == null)
		{
			if (GetMouseButton)
			{
				//_target = GetTarget().character;
				_targetPoint = GetTarget().Position;

				//_target = GetRaycastTarget();
				_targetPoint = GetMousePoint();
			}
			yield return null;
		}
		TargetInfo targetInfo = new TargetInfo();
		targetInfo.AddTarget(_target);
		targetInfo.Points.Add(_targetPoint);
		//callbackDataSaved(targetInfo);
	}
}
