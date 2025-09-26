using Mirror;
using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class IceSword : Skill
{
	[SerializeField] private float _damage = 15f;
	//[SerializeField] private GameObject _basePlayer;
	[SerializeField] private Character _playerLinks;
	[SerializeField] private DeathSpiral _deathSpiral;
	[SerializeField] private SeriesOfStrikes _seriesOfStrikes;
	[SerializeField] private GameObject _sword;


	private int _hitInTheRow = 0;
	private Character _oldtarget;
	private Character _target;
	private float _duration = 3;
	private Energy _energy;
	private RuneComponent _rune;
	private bool _critDmg = false;
	protected override bool IsCanCast => IsCanCastCheck();

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    private bool IsCanCastCheck()
	{
		if (_target == null) return false;

		if (Vector3.Distance(_target.transform.position, transform.position) > Radius)
		{
			return false;
		}
		return true;
	}

	private void Start()
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

	}

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        _target = (Character)targetInfo.Targets[0];
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
	{
		while (_target == null)
		{
			if (GetMouseButton)
			{
				_target = GetRaycastTarget();
			}
			yield return null;
		}
		TargetInfo targetInfo = new TargetInfo();
		targetInfo.Targets.Add(_target);
		callbackDataSaved(targetInfo);
	}

	protected override IEnumerator CastJob()
	{
		_seriesOfStrikes.MakeHit(_target, AbilityForm.Magic, 0, 10, 0);
		if (_target == _oldtarget)
		{
			_hitInTheRow++;
			Debug.Log("hit from sword in a row");
		}
		else
		{
			_hitInTheRow = 1;
			_oldtarget = _target;
			Debug.Log("first hit from sword");
		}
		if (_hitInTheRow > 2)
		{
			_deathSpiral.AddCharge();
			_hitInTheRow = 0;
		}
		ApplyDamage();
		CmdAdd(_target.gameObject);
		yield return null;
	}

	protected override void ClearData()
	{

		_sword.SetActive(false);
		_target = null;
	}

	private void ApplyDamage()
	{
		float energyBonus = Mathf.Min(_energy.CurrentValue, 10);
		_energy.CmdUse(energyBonus);

		float totalDamage = _damage + energyBonus;

		Damage damage2 = new Damage
		{
			Value = totalDamage,
			Type = DamageType.Physical,
			PhysicAttackType = AttackRangeType.RangeAttack,
		};

		if (_critDmg && _target.CharacterState.CheckForState(States.Frozen))
		{
			damage2.Value *= (Random.Range(0, 100) < 15) ? 1.8f : 1.1f;
		}

		CmdApplyDamage(damage2, _target.gameObject);

		_sword.SetActive(true);
		_energy.SumDamageMake(damage2.Value);
		_rune.SumDamageMake(damage2.Value);
	}

	[Command]
	private void CmdAdd(GameObject enemy)
	{
		Character enemyCharacter = enemy.GetComponent<Character>();
		enemyCharacter.CharacterState.AddState(States.Cooling, _duration, 0, _playerLinks.gameObject, name);
	}

	public void TalentCritDmg(bool value)
	{
		_critDmg = value;
	}
}
