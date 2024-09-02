using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class MagicDefenseArea : Projectiles
{
	private float _shieldCapacity = 600;

	private void Start()
	{
		StartCoroutine(DestroyObj());
	}

	[Server]
	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (_dad == null) return;
		if (collision.TryGetComponent<Character>(out var character))
		{
			character.CharacterState.CmdAddState(States.MagicBuff, 10, _shieldCapacity + _energyDad * 30, _dad.gameObject, _skill.name);
		}
	}

	private IEnumerator DestroyObj()
	{
		yield return new WaitForSeconds(10);
	}

}