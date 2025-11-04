using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlagueAbsorption : Skill
{
	[SerializeField] private DeathSpiral _deathSpiral;
	[SerializeField] private HeroComponent _playerLinks;

	private Plague _plagueEnemy;
	private Character _target;
	private int _charges = 0;
	//private Energy _energy;
	//private RuneComponent _rune;
	public int Charges => _charges;

	protected override bool IsCanCast => IsCanCastCheck();

    protected override int AnimTriggerCastDelay => throw new System.NotImplementedException();

    protected override int AnimTriggerCast => throw new System.NotImplementedException();

    private bool IsCanCastCheck()
	{
		if(_target == null) return false;
		return true;
		/*if (_rune.CurrentValue >= 1)
		{
			_rune.CmdUse(1);
			return true;
		}
		else
		{
			return false;
		}*/
	}
	/*private void Start()
	{
		for (int i = 0; i < _playerLinks.Resources.Count; i++)
		{
			if (_playerLinks.Resources[i].Type == ResourceType.Energy)
			{
				_energy = (Energy)_playerLinks.Resources[i];
			}
			if (_playerLinks.Resources[i].Type == ResourceType.Rune)
			{
				_rune = (RuneComponent)_playerLinks.Resources[i];
			}
		}

	}*/
	[Command]
	public void CmdUseCharge(int value)
	{
		if (_charges - value >= 0)
		{
			_charges-= value;
		}
	}

	protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
	{
		while (_target == null && _charges <= 0)
		{
			if (GetMouseButton)
			{
				//Debug.Log("CHECK FOR TEst@@");
				//_target = GetRaycastTarget();
			}
			yield return null;
		}
        Debug.LogError("TargetDataError");
    }

	protected override IEnumerator CastJob()
	{
		Absorption(_target.gameObject);
		yield return null;
	}

	protected override void ClearData()
	{
		_target = null;
	}

	[Command]
	private void Absorption(GameObject target)
	{
		Character enemy = target.GetComponent<Character>(); 
		//_target.CharacterState.CmdAddState(States.Stun, 40, 0, _playerLinks.gameObject, Name);
		/*if (_charges > 0)
		{
			_charges--;
			_rune.CmdAdd(1);
		}
		else*/
		{
			_plagueEnemy = (Plague)enemy.CharacterState.GetState(States.Plague);
			if (_plagueEnemy == null) return;

			if (_plagueEnemy.GetStack >= 0)
			{
				Debug.Log("CHECK FOR TEst@@");
				_charges++;
				//_deathSpiral.TalentAddSuperCharge();
				enemy.CharacterState.RemoveState(States.Plague);
			}
		}
	}

	protected override bool TryPayCost(List<SkillEnergyCost> skillEnergyCosts, bool startCooldown = true)
	{
		if (IsHaveResourceOnSkill)
		{
			if (_charges <= 0)
			{
				foreach (var skillCost in _skillEnergyCosts)
				{
					var resource = _hero.Resources.First(r => r.Type == skillCost.resourceType);
					resource.CmdUse(Buff.ManaCost.GetBuffedValue(skillCost.resourceCost));
				}
				//_firstShot = false;
			}
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

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        Debug.LogError("TargetDataError");
    }
}
