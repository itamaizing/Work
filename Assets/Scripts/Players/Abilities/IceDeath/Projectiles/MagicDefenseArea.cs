using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class MagicDefenseArea : Shield
{
	private float _shieldCapacity = 600;

	private void Start()
	{
		StartCoroutine(DestroyObj());
	}

	[Server]
	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (collision.TryGetComponent<Character>(out var character))
		{
			character.Health.Shields.Add(this);
			//character.CharacterState.CmdAddState(States.MagicBuff, 10, _shieldCapacity + _energyDad * 30, _dad.gameObject, _skill.name);
		}
	}

	[Server]

	private void OnTriggerExit2D(Collider2D collision)
	{
		if (collision.TryGetComponent<Character>(out var character))
		{
			character.Health.Shields.Remove(this);
			//character.CharacterState.CmdAddState(States.MagicBuff, 10, _shieldCapacity + _energyDad * 30, _dad.gameObject, _skill.name);
		}
	}

	private IEnumerator DestroyObj()
	{
		yield return new WaitForSeconds(10);
	}

}