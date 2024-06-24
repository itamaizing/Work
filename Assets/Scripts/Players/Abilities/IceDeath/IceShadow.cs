using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Progress;
using static UnityEngine.GraphicsBuffer;

public class IceShadow : Ability
{
	[Header("Ability properties")]
	[SerializeField] private IceShadowObject _shadow;
	[SerializeField] private Character _playerLinks; 
	[SerializeField] private SeriesOfStrikes _seriesOfStrikes;

	private bool _lastHit = false;
	protected override void Cast()
	{
		//PayCost();
		if (_playerLinks.RuneComponent.RemoveRune(1, this))
		{
			Shoot();
		}
		else
		{
			TryCancel();
		}
	}

	protected override void Cancel()
	{

	}

	private void Shoot()
	{
		Debug.Log("test spawn");
		/*IceShadowObject projectileGm = Instantiate(_shadow, gameObject.transform.position, Quaternion.identity);
		projectileGm.Init(_playerLinks.gameObject ,Mana.Value);*/
		_lastHit = _seriesOfStrikes.MakeHit(null, AbilityForm.Magic, 1);
		CmdCreateProjecttile(0, _playerLinks.Stamina.Value, _lastHit);
		_playerLinks.Stamina.Use(_playerLinks.Stamina.Value);
	}

	[Command]
	private void CmdCreateProjecttile(float angle, float manaValue, bool lastHit)
	{
		IceShadowObject projectile = Instantiate(_shadow, gameObject.transform.position, Quaternion.identity);
		projectile.Init(_playerLinks, manaValue, lastHit);

		NetworkServer.Spawn(projectile.gameObject);

		RpcInit(projectile.gameObject, manaValue, lastHit);
	}

	[ClientRpc]
	private void RpcInit(GameObject obj, float manaValue, bool lastHit)
	{
		obj.GetComponent<IceShadowObject>().Init(_playerLinks, manaValue, lastHit);
	}
	/*private Vector3 InstantiatePoint()
	{
		Vector3 mousePosition = Input.mousePosition;
		//mousePosition.z = 10f; // Set this to the distance from the camera to the object
		Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);
		float distance = Vector3.Distance(gameObject.transform.position, worldPosition);
		//Vector3 spawnPos;
		if(distance <= _radius) 
		{
			return worldPosition;
		}
		else
		{
			Vector3 direction = (worldPosition - gameObject.transform.position).normalized;
			Vector3 spawnPosition = gameObject.transform.position + direction * _radius;
			return spawnPosition;
		}
		
	}*/
}
