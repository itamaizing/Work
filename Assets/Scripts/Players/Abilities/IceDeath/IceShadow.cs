using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IceShadow : Skill
{
	[Header("Ability properties")]
	[SerializeField] private IceShadowObject _shadow;
	[SerializeField] private HeroComponent _playerLinks; 
	[SerializeField] private SeriesOfStrikes _combo;

	private Energy _energy;
	//private RuneComponent _rune;
	private bool _lastHit = false;
	private bool _talentEvade = false;
	private bool _evaded = false;
	private float _evadedTimer = 2f;
	private float _manaUsed = 0;

	protected override bool IsCanCast => IsCanCastCheck();

    protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerCast => 0;

    private bool IsCanCastCheck()
	{
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
	private void Start()
	{
		for (int i = 0; i < _playerLinks.Resources.Count; i++)
		{
			if (_playerLinks.Resources[i].Type == ResourceType.Energy)
			{
				_energy = (Energy)_playerLinks.Resources[i];
			}
			/*if (_playerLinks.Resources[i].Type == ResourceType.Rune)
			{
				_rune = (RuneComponent)_playerLinks.Resources[i];
			}*/
		}

	}

	private void OnEnable()
	{
		_playerLinks.Health.Evaded += Evaded;
	}

	private void OnDestroy()
	{
		_playerLinks.Health.Evaded -= Evaded;
	}

	protected override IEnumerator PrepareJob()
	{
		yield return null;
	}

	protected override IEnumerator CastJob()
	{
		Shoot();
		yield return null;
	}

	protected override void ClearData()
	{
		
	}

	private void Shoot()
	{
		Buff.AttackSpeed.ReductionPercentage(1 + _combo.GetMultipliedSpeed() / 100);
		/*IceShadowObject projectileGm = Instantiate(_shadow, gameObject.transform.position, Quaternion.identity);
		projectileGm.Init(_playerLinks.gameObject ,Mana.Value);*/
		_lastHit = _combo.MakeHit(null, AbilityForm.Magic, 1, 0);

		Buff.AttackSpeed.IncreasePercentage(1 + _combo.GetMultipliedSpeed() / 100);

		_manaUsed = _energy.CurrentValue;
		_energy.CmdUse(_manaUsed);
		CmdCreateProjecttile(0, _manaUsed, _lastHit);
	}

	[Command]
	private void CmdCreateProjecttile(float angle, float manaValue, bool lastHit)
	{
		IceShadowObject projectile = Instantiate(_shadow, gameObject.transform.position, Quaternion.identity);
		SceneManager.MoveGameObjectToScene(projectile.gameObject, _hero.NetworkSettings.MyRoom);
		//var userSettings = gameObject.GetComponentInParent<UserNetworkSettings>();
		//SceneManager.MoveGameObjectToScene(projectile.gameObject, userSettings.MyRoom);

		projectile.Init(_playerLinks, manaValue, lastHit, this);

		NetworkServer.Spawn(projectile.gameObject);

		RpcInit(projectile.gameObject, manaValue, lastHit);
	}

	[ClientRpc]
	private void RpcInit(GameObject obj, float manaValue, bool lastHit)
	{
		obj.GetComponent<IceShadowObject>().Init(_playerLinks, manaValue, lastHit, this);
	}

	public void TalentEvade(bool value)
	{
		_talentEvade = value;
	}

	public void Evaded()
	{
		if( _talentEvade) 
		{
			_evaded = true;
			StartCoroutine(CountDownToTalentEvede());
		}
	}
		
	private IEnumerator CountDownToTalentEvede()
	{
		yield return new WaitForSeconds(_evadedTimer);
		_evaded = false;
	}

	protected override bool TryPayCost(List<SkillEnergyCost> skillEnergyCosts, bool startCooldown = true)
	{
		if (IsHaveResourceOnSkill)
		{
			if (!_evaded || _talentEvade)
			{
				foreach (var skillCost in _skillEnergyCosts)
				{
					var resource = _hero.Resources.First(r => r.Type == skillCost.resourceType);
					resource.CmdUse(Buff.ManaCost.GetBuffedValue(skillCost.resourceCost));
				}
				_evaded = false;
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
}

