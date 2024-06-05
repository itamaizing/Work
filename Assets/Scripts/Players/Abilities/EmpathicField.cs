using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmpathicField : Ability
{
	[SerializeField] private Collider2D _collider;
	private List<HealthComponent> _players;
	private bool _enabled;
	protected override void Cast()
	{
		_enabled = true;
		_collider.enabled = true;
	}

	protected override void Cancel()
	{
		_enabled = false;
		_collider.enabled = false;
	}

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if(collision.TryGetComponent<Character>(out var character))
		{
			_players.Add(character.Health);
		}
	}

	private void OnTriggerExit2D(Collider2D collision)
	{
		if (collision.TryGetComponent<Character>(out var character))
		{
			_players.Remove(character.Health);
		}
	}
}
