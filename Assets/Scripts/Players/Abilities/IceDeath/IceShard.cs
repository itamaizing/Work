using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class IceShard : Skill
{
	[SerializeField] private IceShardProjectile _projectile;
	[SerializeField] private HeroComponent _playerLinks;
	[SerializeField] private SeriesOfStrikes _seriesOfStrikes;

	[SerializeField] private float _baseEnergyCost = 40f;
	[SerializeField] private float _maxAdditionalCost = 10f;

	private Vector3 _mousePos = Vector2.positiveInfinity;
	private bool _talentPlague = false;
	private bool _talentChragesPlague = false;
	private Energy _energy;
	private float _spentEnergyForShard;

	protected override bool IsCanCast => true;

    protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerCast => 0;

    private void Start()
	{
        //_energy = (Energy)_playerLinks.Resources[ResourceType.Energy];
    }

	private void EnsureResources()
	{
		if (_energy == null) _energy = (Energy)Hero.Resources[ResourceType.Energy];
	}

	protected override bool CheckResourcesOnSkill()
	{
		EnsureResources();

		float baseCost = Buff.ManaCost.GetBuffedValue(_baseEnergyCost);
		return _energy.CurrentValue >= baseCost + 1f;
	}

	private bool ConsumeEnergy()
	{
		EnsureResources();

		float baseCost = Buff.ManaCost.GetBuffedValue(_baseEnergyCost);
		float currentEnergy = _energy.CurrentValue;

		if (currentEnergy < baseCost + 1f)	return false;

		float additionalEnergy = Mathf.Min(currentEnergy - baseCost, _maxAdditionalCost);
		additionalEnergy = Mathf.Clamp(additionalEnergy, 1f, _maxAdditionalCost);

		_spentEnergyForShard = baseCost + additionalEnergy;

		_energy.CmdUse(_spentEnergyForShard);
		return true;
	}

	private void Shoot()
	{
		EnsureResources();

		if (!ConsumeEnergy())
		{
			TryCancel(true);
			return;
		}

		Vector3 lookDir = _mousePos - _playerLinks.transform.position;
		float angle = Mathf.Atan2(lookDir.z, lookDir.x) * Mathf.Rad2Deg - 90f;
		_seriesOfStrikes.MakeHit(null, Info.AbilityForm, 1, 5, 3);

		CmdCreateProjecttile(angle, _energy.CurrentValue, _talentPlague, _talentChragesPlague);
	}

	[Command]
	private void CmdCreateProjecttile(float angle, float manaValue, bool talentPlague, bool talentChargesPlague)
	{
		IceShardProjectile projectile = Instantiate(_projectile, gameObject.transform.position, Quaternion.Euler(0, -angle, 0));
		projectile.Init(_playerLinks, manaValue, false, this);
		projectile.Talents(talentPlague, talentChargesPlague);

		NetworkServer.Spawn(projectile.gameObject);

		RpcInit(projectile.gameObject, manaValue, talentPlague, talentChargesPlague);
	}

	[ClientRpc]
	private void RpcInit(GameObject obj, float manaValue, bool talentPlague, bool talentChargesPlague)
	{
		obj.GetComponent<IceShardProjectile>().Init(_playerLinks, manaValue, false, this);
	}

	public void TalentPlague(bool value)
	{
		_talentPlague = value;
	}
	public void TalentChargesPlague(bool value)
	{
		_talentChragesPlague = value;
	}
    public override void LoadTargetData(TargetInfo targetInfo)
    {
        _mousePos = targetInfo.Points[0];
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
	{
		if (_energy == null)
			_energy = (Energy)Hero.Resources[ResourceType.Energy];
        //Debug.Log("MOUSE POS " + float.IsPositiveInfinity(_mousePos.x));
        while (float.IsPositiveInfinity(_mousePos.x))
		{
			if (GetMouseButton)
			{
				_mousePos = Targeting.GetMousePoint();
				/*if (Targeting.GetTarget()?.Character == null)
				{
					_mousePos = Targeting.GetTarget().Position;
				}
				else
				{
					_mousePos = Targeting.GetTarget().Character.transform.position;
				}*/
			}
			yield return null;
		}
		TargetInfo targetInfo = new();
		targetInfo.Points.Add( _mousePos );
		callbackDataSaved( targetInfo );
	}

	protected override IEnumerator CastJob()
	{
		Shoot();
		yield return null;
	}

	protected override void ClearData()
	{
		Debug.Log("CLEARED");
		_mousePos = Vector2.positiveInfinity;
	}
}
