using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class LastBreathCorpse : Ability
{
	[SerializeField] private Character _character;
	private float _timer = 12;
	private bool _isAvaliable = true;
	//private float _cooldown = 12;
	protected override void Cancel()
	{
		
	}

	protected override void Cast()
	{
		if (_isAvaliable)
		{
			_isAvaliable = false;
			_character.CharacterState.CmdAddState(States.LastBreath, 12, 0, _character.gameObject, name);
		}
	}

	private void Update()
	{
		if (!_isAvaliable)
		{
			Timer();
		}
	}

	private void Timer()
	{
		_timer -= Time.deltaTime;
		if (_timer < 0) 
		{
			_isAvaliable = true;
			_timer = _cooldown;
		}
	}
}
