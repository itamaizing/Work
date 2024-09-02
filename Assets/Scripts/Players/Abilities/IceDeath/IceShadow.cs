using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEditor.Progress;

public class IceShadow : Skill
{
	[Header("Ability properties")]
	[SerializeField] private IceShadowObject _shadow;
	[SerializeField] private Character _playerLinks; 
	[SerializeField] private SeriesOfStrikes _combo;

	private bool _lastHit = false;
	private bool _talentEvade = false;
	private bool _evaded = false;
	private float _evadedTimer = 2f;

	protected override bool IsCanCast
	{
		get { return true; }
	}

	private void OnEnable()
	{
		_playerLinks.Health.Evaded += Evaded;
	}
	private void OnDestroy()
	{
		_playerLinks.Health.Evaded -= Evaded;
	}
	protected override IEnumerator PrepareJob()
	{
		yield return null;
	}

	protected override IEnumerator CastJob()
	{
		//if (_playerLinks.RuneComponent.RemoveRune(1, this))
		_playerLinks.RuneComponent.CmdUse(1);
		Shoot();
		yield return null;
	}

	protected override void ClearData()
	{
		
	}

	/*protected override void Cast()
	{
		PayCost();
		if(_talentEvade &&  _evaded) 
		{
			Shoot();
		}
		//else //if (_playerLinks.RuneComponent.RemoveRune(1, this))
		{
			Shoot();
		}
		//else
		{
			TryCancel();
		}
	}

	protected override void Cancel()
	{

	}*/

	private void Shoot()
	{
		Buff.AttackSpeed.ReductionPercentage(1 + _combo.GetMultipliedSpeed() / 100);

		Debug.Log("test spawn");
		/*IceShadowObject projectileGm = Instantiate(_shadow, gameObject.transform.position, Quaternion.identity);
		projectileGm.Init(_playerLinks.gameObject ,Mana.Value);*/
		_lastHit = _combo.MakeHit(null, AbilityForm.Magic, 1, 0);

		Buff.AttackSpeed.IncreasePercentage(1 + _combo.GetMultipliedSpeed() / 100);

		CmdCreateProjecttile(0, _playerLinks.Stamina.CurrentValue, _lastHit);
		_playerLinks.Stamina.TryUse(_playerLinks.Stamina.CurrentValue);
	}

	[Command]
	private void CmdCreateProjecttile(float angle, float manaValue, bool lastHit)
	{
		IceShadowObject projectile = Instantiate(_shadow, gameObject.transform.position, Quaternion.identity);
		SceneManager.MoveGameObjectToScene(projectile.gameObject, _hero.NetworkSettings.MyRoom);
		//var userSettings = gameObject.GetComponentInParent<UserNetworkSettings>();
		//SceneManager.MoveGameObjectToScene(projectile.gameObject, userSettings.MyRoom);

		projectile.Init(_playerLinks, manaValue, lastHit);

		NetworkServer.Spawn(projectile.gameObject);

		RpcInit(projectile.gameObject, manaValue, lastHit);
	}

	[ClientRpc]
	private void RpcInit(GameObject obj, float manaValue, bool lastHit)
	{
		obj.GetComponent<IceShadowObject>().Init(_playerLinks, manaValue, lastHit);
	}

	public void TalentEvade(bool value)
	{
		_talentEvade = value;
	}
	public void Evaded()
	{
		if( _talentEvade) 
		{
			_evaded = true;
			StartCoroutine(CountDownToTalentEvede());
		}
	}
	private IEnumerator CountDownToTalentEvede()
	{
		yield return new WaitForSeconds(_evadedTimer);
		_evaded = false;
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

