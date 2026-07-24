using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class IceShard : Skill,IEnergyDamagable
{
	[SerializeField] private IceShardProjectile _projectile;
	[SerializeField] private HeroComponent _playerLinks;

	[SerializeField] private float _baseEnergyCost = 5f;
	[SerializeField] private float _maxAdditionalCost = 0f;

	private Vector3 _mousePos = Vector2.positiveInfinity;
	private bool _talentPlague = false;
	private bool _talentChragesPlague = false;
	private Energy _energy;
	private float _spentEnergyForShard;

	protected override bool IsCanCast => true;

	protected override int AnimTriggerCastDelay => Animator.StringToHash("Throw");

	protected override int AnimTriggerCast => 0;

    public override void Init(SkillRenderer render, Character hero)
    {
		base.Init(render, hero);
		_energy = (Energy)hero.Resources[ResourceType.Energy];
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
		_mousePos = Targeting.GetTarget().Poisition;
		Vector3 lookDir = _mousePos - _playerLinks.transform.position;
		lookDir.y = 0f;
		lookDir.Normalize();

		float angle = Mathf.Atan2(lookDir.z, lookDir.x) * Mathf.Rad2Deg - 90f;

		CmdCreateProjecttile(angle, _energy.CurrentValue, _talentPlague, _talentChragesPlague, 4f);
	}

	[Command]
	private void CmdCreateProjecttile(float angle, float manaValue, bool talentPlague, bool talentChargesPlague, float maxDistance)
	{
		IceShardProjectile projectile = Instantiate(_projectile, transform.position, Quaternion.Euler(0, -angle, 0));
    
		projectile.Init(_hero, manaValue, false, this);
		projectile.Talents(talentPlague, talentChargesPlague);
		projectile.SetMaxDistance(maxDistance);

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

	public bool IsStreamSkill { get; }
	public bool IsFrostEnergyApplied { get; }
}
