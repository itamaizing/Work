using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class BlockOfIce : Skill,IEnergyDamagable
{
	[SerializeField] private BlockOfIceProjectile _iceArrow;
	[SerializeField] private HeroComponent _playerLinks;
	[SerializeField] private SeriesOfStrikes _seriesOfStrikes;
	[SerializeField] private float _runeCost = 1f;
	[SerializeField] private float _energyStep = 5f;
	[SerializeField] private float _damagePerStep = 3f;
	[SerializeField] private float _maxEnergySpend = 25f;

	private Vector3 _mousePos = Vector3.positiveInfinity;
	private Energy _energy;

	protected override bool IsCanCast => IsCanCastCheck();

    protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerCast => 0;

    private bool IsCanCastCheck()
	{
		return true;
	}

	private void Start()
	{
        //_energy = (Energy)_playerLinks.Resources[ResourceType.Energy];
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
	    if (targetInfo.Points.Count > 0)
		    _mousePos = targetInfo.Points[0];
    }

	private void Shoot(float bonusDamage)
	{
		Vector3 lookDir = _mousePos - _playerLinks.transform.position;
		float angle = Mathf.Atan2(lookDir.z, lookDir.x) * Mathf.Rad2Deg - 90f;

		CmdCreateProjecttile(angle, bonusDamage);
		_seriesOfStrikes.MakeHit(null, AbilityForm.Magic, 1, 0, 0);
	}

	[Command]
	private void CmdCreateProjecttile(float angle, float bonusDamage)
	{
		BlockOfIceProjectile projectile = Instantiate(_iceArrow, transform.position, Quaternion.Euler(0, -angle, 0));

		float finalDamage = Damage + bonusDamage;

		projectile.Init(_playerLinks, finalDamage, false, this);

		NetworkServer.Spawn(projectile.gameObject);

		RpcInit(projectile.gameObject, finalDamage);
	}

	[ClientRpc]
	private void RpcInit(GameObject obj, float manaValue)
	{
		obj.GetComponent<BlockOfIceProjectile>().Init(_playerLinks, manaValue, false, this);
	}

	protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
	{
		if (_energy == null)
			_energy = (Energy)Hero.Resources[ResourceType.Energy];

		while (float.IsPositiveInfinity(_mousePos.x))
		{
			if (GetMouseButton)
				_mousePos = Targeting.GetMousePoint();

			yield return null;
		}

		TargetInfo targetInfo = new TargetInfo();
		targetInfo.Points.Add(_mousePos);
		callbackDataSaved(targetInfo);
	}

	protected override IEnumerator CastJob()
	{
		if (!Cost.TryPaySingle(_runeCost, ResourceType.Rune, shouldModify: false))
		{
			TryCancel(true);
			yield break;
		}

		float bonusDamage = CalculateBonusDamage();

		Shoot(bonusDamage);

		yield return null;
	}

	private float CalculateBonusDamage()
	{
		float totalBonusDamage = 0f;

		for (int i = 0; i < _maxEnergySpend / _energyStep; i++)
		{
			if (!Cost.TryPaySingle(_energyStep, ResourceType.Energy, shouldModify: false))
				break;

			totalBonusDamage += _damagePerStep;
		}

		return totalBonusDamage;
	}

	protected override void ClearData()
	{
		_mousePos = Vector3.positiveInfinity;
	}

	public bool IsStreamSkill { get; }
	public bool IsFrostEnergyApplied { get; }
}
