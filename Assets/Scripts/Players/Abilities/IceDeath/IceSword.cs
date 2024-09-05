using System.Collections;
using UnityEngine;

public class IceSword : Skill
{
	[SerializeField] private float _damage = 15f;
	//[SerializeField] private GameObject _basePlayer;
	[SerializeField] private Character _playerLinks;
	[SerializeField] private DeathSpiral _deathSpiral;
	[SerializeField] private SeriesOfStrikes _seriesOfStrikes;


	private int _hitInTheRow = 0;
	private Character _oldtarget;
	private Character _target;
	private Energy _energy;

	protected override bool IsCanCast
	{
		get 
		{
			if (_energy.CurrentValue >= 10)
			{
				_energy.TryUse(10);
				return true;
			}
			else
			{ 
				return false; 
			}
		}
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

	protected override IEnumerator PrepareJob()
	{
		while (_target == null)
		{
			if (Input.GetMouseButton(0))
			{
				_target = GetRaycastTarget();
			}
			yield return null;
		}
	}

	protected override IEnumerator CastJob()
	{
		_seriesOfStrikes.MakeHit(_target, AbilityForm.Magic, 0, 0);
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

		yield return null;
	}

	protected override void ClearData()
	{
		throw new System.NotImplementedException();
	}

	private void ApplyDamage()
	{
		Damage damage2 = new Damage
		{
			Value = _damage,
			Type = DamageType.Physical,
			Range = AttackRangeType.RangeAttack,
		};
		//_skill.CmdApplyDamage(damage, target.gameObject);
		_target.Health.TryTakeDamage(ref damage2, this);
	}
}
