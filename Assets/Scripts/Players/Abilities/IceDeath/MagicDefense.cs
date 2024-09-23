using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class MagicDefense : Skill
{
	[SerializeField] private PlagueAbsorption _plagueAbsorption;
	[SerializeField] private DeathSpiral _deathSpiral;
	[SerializeField] private HeroComponent _playerLinks;
	[SerializeField] private MagicDefenseArea _magDefArea;

	private Character _target;
	private float _shieldCapacity = 200;
	private bool _isArea = false;
	private Vector2 _position = Vector2.positiveInfinity;
	private Energy _energy;
	private RuneComponent _rune;

	protected override bool IsCanCast => CheckCanCast();

	private bool CheckCanCast()
	{
		return Vector3.Distance(_target.transform.position, transform.position) <= Radius;		
	}

	private void Start()
	{
		for (int i = 0; i < _playerLinks.Resources.Count; i++)
		{
			if (_playerLinks.Resources[i].Type == ResourceType.Energy)
			{
				_energy = (Energy)_playerLinks.Resources[i];
			}
		}

	}

	//[Command]
	private void ServerAdd(GameObject obj)
	{
		Character target = obj.GetComponent<Character>();
		target.CharacterState.CmdAddState(States.MagicBuff, 6, _energy.CurrentValue * 10 + _shieldCapacity, _playerLinks.gameObject, name);
	}

	[Command]
	private void SpawnArea(Vector3 position)
	{
		MagicDefenseArea area = Instantiate(_magDefArea, position, Quaternion.identity);
		//area.Init(_playerLinks, _energy.CurrentValue, false, this);
		_energy.TryUse(_energy.CurrentValue);
		NetworkServer.Spawn(area.gameObject);

		RpcInit(area.gameObject, position);
	}
	

	[ClientRpc]
	private void RpcInit(GameObject area, Vector3 position)
	{
		MagicDefenseArea magArea = area.GetComponent<MagicDefenseArea>();
		//magArea.Init(_playerLinks, _energy.CurrentValue, false, this);
	}

	protected override IEnumerator PrepareJob()
	{
		while(_target == null && Vector2.Distance(_position, transform.position) > Radius)
		{
			_target = GetRaycastTarget(transform);
			_position = GetMousePoint();
		}
		if(_deathSpiral.Chargers >= 1 && _plagueAbsorption.Chargers >= 1 && _energy.CurrentValue >= 70 && _rune.CurrentValue >= 1) 
		{

		}
		yield return null;
	}

	protected override IEnumerator CastJob()
	{

		yield return null;
	}

	protected override void ClearData()
	{
		_target = null;
		_position = Vector2.positiveInfinity;
	}

	[Command]
	private void CmdAddShield()
	{
	//	_playerLinks.Health.Shields.Add(_shield);
		Debug.Log(_playerLinks.Health.Shields.Count);
	}

	[Command]
	private void CmdRemoveShield()
	{
	//	_playerLinks.Health.Shields.Remove(_shield);
		Debug.Log(_playerLinks.Health.Shields.Count);
	}

	protected override bool TryPayCost(List<SkillEnergyCost> skillEnergyCosts, bool startCooldown = true)
	{
		if (IsHaveResourceOnSkill)
		{
			/*if (_firstShot)
			{
				foreach (var skillCost in _skillEnergyCosts)
				{
					var resource = _hero.Resources.First(r => r.Type == skillCost.resourceType);
					resource.CmdUse(Buff.ManaCost.GetBuffedValue(skillCost.resourceCost));
				}
				_firstShot = false;
			}*/
			if (startCooldown)
				IncreaseSetCooldown(CooldownTime);

			TryUseCharge();
			return true;
		}
		else
		{
			return false;
		}
	}
}
