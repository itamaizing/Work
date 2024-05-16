using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class IcePuddle : Ability
{
	[SerializeField] private IcePuddleObject _puddle;
	[SerializeField] private PlayerLinks _playerLinks;
	//[SerializeField] private Rigidbody2D _rb;
	//[SerializeField] private HealthPlayer _healthPlayer;
	//[SerializeField] private RunePlayer _rune;

	protected override void Cast()
	{
		PayCost();
		if (_playerLinks.RunePlayer.RemoveRune(1, this))
		{
			Shoot();
		}
	}

	protected override void Cancel()
	{
		//вроде не было нужды для отмены каста, пока что....
	}
	private void Shoot()
	{
		IcePuddleObject puddle = Instantiate(_puddle, InstantiatePoint(), Quaternion.identity);
		puddle.dad = _playerLinks.gameObject;
		puddle.energyPlayer = (EnergyPlayer)Mana;
		puddle.healthPlayer = _playerLinks.HealthPlayer;
	}

	private Vector3 InstantiatePoint()
	{
		Vector3 mousePosition = Input.mousePosition;
		//mousePosition.z = 10f; // Set this to the distance from the camera to the object
		Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);
		float distance = Vector3.Distance(gameObject.transform.position, worldPosition);
		//Vector3 spawnPos;
		if (distance <= _radius)
		{
			return worldPosition;
		}
		else
		{
			Vector3 direction = (worldPosition - gameObject.transform.position).normalized;
			Vector3 spawnPosition = gameObject.transform.position + direction * _radius;
			return spawnPosition;
		}

	}
}
