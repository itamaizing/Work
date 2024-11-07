using Mirror;
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
	private float _duration = 3;
	//private Energy _energy;
	protected override bool IsCanCast => IsCanCastCheck();

    protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerCast => 0;

    private bool IsCanCastCheck()
	{
		return true;
	}

	/*private void Start()
	{
		for (int i = 0; i < _playerLinks.Resources.Count; i++)
		{
			if (_playerLinks.Resources[i].Type == ResourceType.Energy)
			{
				_energy = (Energy)_playerLinks.Resources[i];
			}
		}

	}*/

	protected override IEnumerator PrepareJob()
	{
		while (_target == null)
		{
			if (GetMouseButton)
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
		CmdAdd(_target.gameObject);
		yield return null;
	}

	protected override void ClearData()
	{
		_target = null;
	}

	private void ApplyDamage()
	{
		Damage damage2 = new Damage
		{
			Value = _damage,
			Type = DamageType.Physical,
			PhysicAttackType = AttackRangeType.RangeAttack,
		};
		//_skill.CmdApplyDamage(damage, target.gameObject);
		CmdApplyDamage(damage2, _target.gameObject);
		//_target.CharacterState.CmdAddState(States.Cooling, _duration, 0, _playerLinks.gameObject, name);
		//_target.Health.TryTakeDamage(ref damage2, this);
	}

	[Command]
	private void CmdAdd(GameObject enemy)
	{
		Character enemyCharacter = enemy.GetComponent<Character>();
		enemyCharacter.CharacterState.AddState(States.Cooling, _duration, 0, _playerLinks.gameObject, name);
	}
}
