using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class IceShield : Skill
{
	[SerializeField] private float _percentOfShield = 0.9f;
	[SerializeField] private float _decreaseSpeed = 0.2f;
	[SerializeField] private HeroComponent _playerLinks;
	[SerializeField] private SeriesOfStrikes _combo;
	[SerializeField] private IceShieldObject _shield;

	private bool _active = false;
	private float _timer = 1f;
	private float _delay = 1f;
	private Energy _energy;

	protected override bool IsCanCast => true;

	private void Shoot() 
	{
		_active = !_active;
		Debug.Log(_playerLinks.Health.Shields.Count);
		if (_active) 
		{
			_shield.gameObject.SetActive(true);
			_playerLinks.Move.ChangeMoveSpeed(0.8f);
			CmdAddShield();
		}
		else
		{
			_shield.gameObject.SetActive(false);
			_playerLinks.Move.ChangeMoveSpeed(1.25f);
			CmdRemoveShield();
		}
	}

	/*private void Timer()
	{
		if (_active)
		{
			_timer -= Time.deltaTime;
			if (_timer > 0) return;

		/*	if (_character.Stamina.TryUse(1))
			{
				_timer = _delay;
			}
			else
			{
				_active = false;
			}
		
		}
	}*/

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

	[Command]
	private void CmdAddShield()
	{
		_playerLinks.Health.Shields.Add(_shield);
		Debug.Log(_playerLinks.Health.Shields.Count);
	}

	[Command]
	private void CmdRemoveShield()
	{
		_playerLinks.Health.Shields.Remove(_shield);
		Debug.Log(_playerLinks.Health.Shields.Count);
	}
}

