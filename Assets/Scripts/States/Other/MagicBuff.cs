using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagicBuff : AbstractCharacterState, IDamageable
{
	private Character _character;
	private float _shieldCapacity;

	public event Action<Damage, Skill> DamageTaken;
	public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
	public override States State => States.MagicBuff;
	public override StateType Type => StateType.Magic;
	public override List<StatusEffect> Effects => throw new System.NotImplementedException();

    public Transform transform => throw new NotImplementedException();
    public GameObject gameObject => throw new NotImplementedException();

    protected override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
	{
		_character = character.Character;
		_shieldCapacity = damageToExit;
		_character.Health.Shields.Add(this);
		//_character.Health.SetMagAbsorb(_shieldCapacity);
	}

	public override void UpdateState()
	{
		if (_shieldCapacity <= 0)
		{
			GlobalExit();
		}
	}

	protected override void ExitState()
	{
		_character.Health.Shields.Remove(this);
		characterState.RemoveStateFromList(this);
		//_character.Health.SetMagAbsorb(0);
	}

	/*public override bool Stack(float time)
	{
		duration = time;
		return true;
	}*/

	public bool TryTakeDamage(ref Damage damage, Skill skill)
	{
		_shieldCapacity -= damage.Value;
		if(_shieldCapacity < 0)
		{
			GlobalExit();
		}

		return true;
	}

	public void ShowPhantomValue(Damage phantomValue)
	{
		//throw new NotImplementedException();
	}
}
