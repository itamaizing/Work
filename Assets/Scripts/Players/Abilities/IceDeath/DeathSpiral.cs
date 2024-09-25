using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathSpiral : Skill
{
	[SerializeField] private DeathSpiralProjectile _projectile;
	[SerializeField] private HeroComponent _playerLinks;
	[SerializeField] private SeriesOfStrikes _seriesOfStrikes;
	[SerializeField] private SpawnComponent _spawnComponent;
	[SerializeField] private PlagueAbsorption _plagueAbsorption;

	private float _timer = 1f;
	private Vector2 _mousePos = Vector3.positiveInfinity;
	private bool _superCharge = false;
	private bool _inTheRow = false;
	private bool _talentSecondAttack = false;
	private bool _talentBoostHPBOdy = false;
	private bool _talentHitState = false;
	private bool _talentPlague = false;
	private bool _talentChragesPlague = false;
	private bool _talentCorpseDeath = false;
	private bool _talentCorpseBoostExplode;
	private bool _firstShot = true;

	//private RuneComponent _rune;

	protected override bool IsCanCast => true;

	/*private void Start()
	{
		for (int i = 0; i < _playerLinks.Resources.Count; i++)
		{
			if (_playerLinks.Resources[i].Type == ResourceType.Rune)
			{
				_rune = (RuneComponent)_playerLinks.Resources[i];
			}
		}

	}*/

	private void Update()
	{
		Timer();
	}
	protected override IEnumerator PrepareJob()
	{
		while (float.IsPositiveInfinity(_mousePos.x))
		{
			if (Input.GetMouseButton(0))
			{
				//_playerLinks.RuneComponent.CmdUse(1);
				_mousePos = GetMousePoint();
			}
			yield return null;
		}
	}

	protected override IEnumerator CastJob()
	{
		if(_plagueAbsorption.UseCharge(1))
		{
			PlagueAbsorptionCharge();
		}
		else if (_inTheRow && _talentSecondAttack)
		{
			SecondAttact();
		}
		else
		{
			BasicShoot();
		}
		yield return null;
	}
	protected override void ClearData()
	{
		_mousePos = Vector3.positiveInfinity;
	}

	/*protected override void Cast()
	{
		if(_plagueAbsorption.UseCharge(1))
		{
			_superCharge = true;
			_inTheRow = true;

			RaycastHit2D[] rayHit = Physics2D.RaycastAll(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero, 99, _targetsLayers);

			foreach (var item in rayHit)
			{
				if (rayHit.Length > 0 && item.transform.TryGetComponent<Character>(out Character enemy))
				{
					if (enemy == _playerLinks)
					{
						if (_inTheRow)
						{
							_playerLinks.Health.Heal(10);
							return;
						}
						else
						{
							_playerLinks.Health.Heal(20);
							return;
						}
					}
				}
			}

			_mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			Vector2 lookDir = _mousePos - _playerLinks.Rigidbody2D.position;
			float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
			_seriesOfStrikes.MakeHit(null, AbilityForm.Magic, 1, 0);
			Debug.Log("SUPER CHARGE TEST");
			//Shoot(angle, _inTheRow);
		}

		else if (_inTheRow && _talentSecondAttack)
		{
			_superCharge = false;
			_mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			Debug.LogError("fix");
			Vector2 lookDir = _mousePos - _playerLinks.Rigidbody2D.position;
			float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
			_seriesOfStrikes.MakeHit(null, AbilityForm.Magic, 1, 0);
			Shoot(angle, _inTheRow);
		}
		//else if (_playerLinks.RuneComponent.RemoveRune(2, this))
		{
			_superCharge = false;
			_currentChargers--;
			_inTheRow = true;
			_mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			Vector2 lookDir = _mousePos - _playerLinks.Rigidbody2D.position;
			float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
			_seriesOfStrikes.MakeHit(null, AbilityForm.Magic, 1, 0);
			Shoot(angle, _inTheRow);
		}
		
	}*/

	[Command]
	private void Shoot(float angle, bool inTheRow)
	{		
		DeathSpiralProjectile projectile = Instantiate(_projectile, gameObject.transform.position, Quaternion.Euler(0, 0, angle));
		SceneManager.MoveGameObjectToScene(projectile.gameObject, _hero.NetworkSettings.MyRoom);
		projectile.Init(_playerLinks, 0, false, this);
		projectile.Talents(_talentBoostHPBOdy, _talentHitState, inTheRow, _talentPlague, _talentChragesPlague, _superCharge);
		projectile.Talents(_talentCorpseDeath, _talentCorpseBoostExplode);
		//projectile.TalentBoostHp(_talentBoostHPBOdy);
		//projectile.TalentHitState(_talentHitState);

		NetworkServer.Spawn(projectile.gameObject);

		RpcInit(projectile.gameObject);
		_superCharge = false;
	}

	[ClientRpc]
	private void RpcInit(GameObject obj)
	{
		DeathSpiralProjectile projectile = obj.GetComponent<DeathSpiralProjectile>();
		projectile.Init(_playerLinks, 0, false, this);
		projectile.Talents(_talentBoostHPBOdy, _talentHitState, _inTheRow, _talentPlague, _talentChragesPlague, _superCharge);
		projectile.Talents(_talentCorpseDeath, _talentCorpseBoostExplode);
		_superCharge = false;
	}

	private void PlagueAbsorptionCharge()
	{
		_superCharge = true;
		_inTheRow = true;

		RaycastHit2D[] rayHit = Physics2D.RaycastAll(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero, 99, _targetsLayers);

		foreach (var item in rayHit)
		{
			if (rayHit.Length > 0 && item.transform.TryGetComponent<Character>(out Character enemy))
			{
				if (enemy == _playerLinks)
				{
					if (_inTheRow)
					{
						_playerLinks.Health.Heal(10);
						return;
					}
					else
					{
						_playerLinks.Health.Heal(20);
						return;
					}
				}
			}
		}
		Vector2 lookDir = _mousePos - (Vector2)_playerLinks.transform.position;
		float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
		_seriesOfStrikes.MakeHit(null, AbilityForm.Magic, 1, 0);
		Shoot(angle, _inTheRow);
	}

	private void BasicShoot()
	{
		_firstShot = false;
		_superCharge = false;
		Chargers--;
		_inTheRow = true;
		Vector2 lookDir = _mousePos - (Vector2)_playerLinks.transform.position;
		float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
		_seriesOfStrikes.MakeHit(null, AbilityForm.Magic, 1, 0);
		Shoot(angle, _inTheRow);
	}

	private void SecondAttact()
	{
		_superCharge = false;
		Vector2 lookDir = _mousePos - (Vector2)_playerLinks.transform.position;
		float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
		_seriesOfStrikes.MakeHit(null, AbilityForm.Magic, 1, 0);
		Shoot(angle, _inTheRow);
	}

	public void AddCharge()
	{
		Debug.Log("ADDED CHARGE!!!");
		if(Chargers < _maxCharges)
			Chargers++;
	}

	private void Timer()
	{
		if (!_inTheRow) return;

		_timer-= Time.deltaTime;
		if(_timer <= 0)
		{
			_firstShot = true;
			_inTheRow = false;
			_timer = 1; 
		}
	}

	protected override bool TryPayCost(List<SkillEnergyCost> skillEnergyCosts, bool startCooldown = true)
	{
		if (IsHaveResourceOnSkill)
		{
			if (_firstShot)
			{
				foreach (var skillCost in _skillEnergyCosts)
				{
					var resource = _hero.Resources.First(r => r.Type == skillCost.resourceType);
					resource.CmdUse(Buff.ManaCost.GetBuffedValue(skillCost.resourceCost));
				}
				_firstShot= false;
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

	public void TalentMaxCharges(int maxChargesValue)
	{
		_maxCharges = maxChargesValue;
	}

	public void TalentSecondAttack(bool value)
	{
		_talentSecondAttack = value;
	}

	public void TalentBoostHpCorpse(bool value)
	{
		_talentBoostHPBOdy = value;
	}

	public void TalentHitState(bool value)
	{
		_talentHitState = value;
	}

	public void TalentPlague(bool value)
	{
		_talentPlague = value;
	}

	public void TalentChargesPlague(bool value)
	{
		_talentChragesPlague = value;
	}

	public void TalentCosrpseDeath(bool value)
	{
		_talentCorpseDeath = value;
	}

	public void TalentCorpseBoostExplode(bool value)
	{
		_talentCorpseBoostExplode = value;
	}
}
