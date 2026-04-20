using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class CircularFrosting : Skill
{
	//[SerializeField] private CircularFrostingObject _circle;
	//[SerializeField] private FrostingFrozenTalant _talant;
	[SerializeField] private SeriesOfStrikes _seriesOfStrikes;

	private float _baseDuration = 2;
	private float _duration = 2;
	private Energy _energy;
	private bool _talentFrostingFrozen;

	private const float FrostEnergyCoolingBonusPerStack = 1f;
	private const float FrostEnergyFrostingBonusPerStack = 5f;
	private const float FrostEnergyFrozenBonusPerStack = 10f;

	protected override bool IsCanCast => true;

	protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerCast => 0;

    private void Start()
	{
        //_energy = (Energy)Hero.Resources[ResourceType.Energy];

    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {

    }

    protected override IEnumerator CastJob()
	{
		CreateSmoke();
		yield return null;
	}

	protected override void ClearData()
	{
		
	}

	protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
	{
		if (_energy == null)
			_energy = (Energy)Hero.Resources[ResourceType.Energy];

        callbackDataSaved(null);
		yield return null;
	}

	private void CreateSmoke()
	{
		Collider[] enemyDetected = Physics.OverlapSphere(transform.position, AreaInfo.Radius);
		float usedEnergy = 0;
		if (_energy.CurrentValue >= 30)
		{
			_duration = _baseDuration + 3;
			usedEnergy = 30;
			_energy.CmdUse(30);
		}
		else
		{
			_duration = _baseDuration + _energy.CurrentValue / 10;
			usedEnergy = _energy.CurrentValue;
			_energy.CmdUse(_energy.CurrentValue);
		}
		foreach (var enemy in enemyDetected) 
		{
			Debug.Log(enemy);
			if (enemy.TryGetComponent<Character>(out var enemyCharacter))
			{
				if (enemyCharacter != Hero)
				{
					_seriesOfStrikes.MakeHit(enemyCharacter, Info.AbilityForm, 1, usedEnergy, 0);
					CmdAdd(enemy.gameObject);
					//enemyCharacter.CharacterState.CmdAddState(States.Frosting, _duration, 0, _playerLinks.gameObject, name);
				}
				/*if (_talant != null)
				{
					if (_talant.IsActive)
					{
						enemyCharacter.CharacterState.CmdAddState(States.Frozen, _duration, 0);
						//enemyCharacter.CharacterState.AddState(new FrozenState(), _duration, 0, States.Frozen);
					}
				}*/
			}
		}
		//var smoke = Instantiate(_circle, transform);
		//smoke.dad = _links;
		//_canCast = false;
	}

	[Command]
	private void CmdAdd(GameObject enemy)
	{
		if (enemy == null) return;

		Character enemyCharacter = enemy.GetComponent<Character>();
		if (enemyCharacter == null) return;

		if (_talentFrostingFrozen && enemyCharacter.CharacterState.CheckForState(States.Frosting))
		{
			ApplyStateWithFrostEnergyBonus(enemyCharacter, States.Frozen, _duration);
		}

		ApplyStateWithFrostEnergyBonus(enemyCharacter, States.Frosting, _duration);
	}

	private void ApplyStateWithFrostEnergyBonus(Character target, States state, float duration)
	{
		if (target == null || target.CharacterState == null)
			return;

		bool hasFrostEnergy = target.CharacterState.CheckForState(States.FrostEnergy);

		int currentStacks = target.CharacterState.CheckStateStacks(state);
		int stacksAfterApply = currentStacks + 1;

		float bonusPerStack = 0f;

		switch (state)
		{
			case States.Cooling:
				bonusPerStack = FrostEnergyCoolingBonusPerStack;
				break;

			case States.Frosting:
				bonusPerStack = FrostEnergyFrostingBonusPerStack;
				break;

			case States.Frozen:
				bonusPerStack = FrostEnergyFrozenBonusPerStack;
				break;
		}

		if (hasFrostEnergy && bonusPerStack > 0f)
		{
			float bonusDamage = stacksAfterApply * bonusPerStack;

			Damage bonus = new Damage
			{
				Value = bonusDamage,
				Type = DamageType.Magical
			};

			target.Health.TryTakeDamage(ref bonus, this);
		}

		target.CharacterState.AddState(state, duration, 0, Hero.gameObject, name);
	}

	public void SetTalentFrostingFrozen(bool value)
	{
		_talentFrostingFrozen = value;
	}
}
