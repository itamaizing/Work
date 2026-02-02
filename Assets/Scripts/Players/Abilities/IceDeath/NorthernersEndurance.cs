using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class NorthernersEndurance : Skill
{
	[SerializeField] private Character _playerLinks;
	private Character _target;
	private Energy _energy;

	protected override bool IsCanCast => true;

    protected override int AnimTriggerCastDelay => throw new System.NotImplementedException();

    protected override int AnimTriggerCast => throw new System.NotImplementedException();

    private void Start()
	{
        _energy = (Energy)_playerLinks.Resources[ResourceType.Energy];
	}

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        _target = (Character)targetInfo.GetTargets()[0];
    }

    protected override IEnumerator CastJob()
	{
		float boostHp = 0.1f + 0.003f * _energy.CurrentValue;
		_energy.CmdUse(_energy.CurrentValue);
		Shoot(boostHp, _target.gameObject);
		yield return null;
	}

	protected override void ClearData()
	{
		_target = null;
	}

	protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
	{
		while (_target == null)
		{
			if (GetMouseButton)
			{
			//	_target = GetRaycastTarget(true);
			}
			yield return null;
		}
		TargetInfo targetInfo = new();
		targetInfo.AddTarget(_target);
		callbackDataSaved(targetInfo);
	}

	[Command]
	private void Shoot(float boostHp, GameObject targetGm)
	{
		Character target = targetGm.GetComponent<Character>();

		/*float boostHp = 0.1f + 0.003f * _energy.CurrentValue;
		_energy.CmdUse(_energy.CurrentValue);*/
		target.CharacterState.AddState(States.NorthernerEndurance, 6, boostHp, _playerLinks.gameObject, name);
		
	}
}
