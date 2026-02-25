using Mirror;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IceCloud : Skill
{
	[SerializeField] private IceCloudProjectile _projectile;
	[SerializeField] private SeriesOfStrikes _combo;
	[SerializeField] private AudioClip _audioClip;

	private Vector3 _mousePos = Vector3.positiveInfinity;
    private Vector3 _mousePos2 = Vector2.positiveInfinity;
    private AudioSource _audioSource;
	private bool _boostDmg;
	private bool _lastHit;
	private Energy _energy;
	private bool _frozwenTalent;
	private float _baseDamage = 10f;

	#region Constants
	private const float ProjectileSpawnOffset = 0.2f;
	private const float AngleOffset = 90f;
	private const float ComboSpeedDivider = 100f;
	private const float EnergyPerDamageUnit = 5f;
	#endregion

	protected override bool IsCanCast
	{
		get
		{
			if (Targeting.GetTarget().Character != null) return Vector3.Distance(Targeting.GetTarget().Character.transform.position, transform.position) <= AreaInfo.Radius;

			else return true;
		}
	}

	protected override int AnimTriggerCastDelay => 0;

	protected override int AnimTriggerCast => Animator.StringToHash("IceCloud");

    private void OnDestroy()
    {
        OnSkillCanceled -= HandleSkillCanceled;
    }

    private void OnEnable()
    {
        OnSkillCanceled += HandleSkillCanceled;
    }

    private void HandleSkillCanceled()
    {
		CanMoveIceCloud();
		Targeting.ClearTarget();
		_mousePos = Vector3.positiveInfinity;
	}

    private void Start()
	{
		_audioSource = GetComponent<AudioSource>();

        _energy = (Energy)Hero.Resources[ResourceType.Energy];
    }

    private void Shoot()
	{
		Vector3 lookDir = _mousePos - Hero.transform.position;
		float angle = Mathf.Atan2(lookDir.z, lookDir.x) * Mathf.Rad2Deg - AngleOffset;
		if (_combo.MakeHit(null, AbilityForm, 1, 0, 0, _combo.GetMultipliedSpeed() / ComboSpeedDivider)) _lastHit = true;

		float energyToUse = _energy.CurrentValue;
		_energy.CmdUse(energyToUse);

		CmdCreateProjecttile(angle, energyToUse, lookDir.normalized);

		Targeting.ClearTarget();
		_mousePos = Vector2.positiveInfinity;
		ClearData();
	}

	[Command]
	private void CmdCreateProjecttile(float angle, float manaValue, Vector3 lookDir)
    {
		IceCloudProjectile projectile = Instantiate(_projectile, gameObject.transform.position + lookDir * ProjectileSpawnOffset, Quaternion.Euler(0, -angle, 0));
		SceneManager.MoveGameObjectToScene(projectile.gameObject, _hero.NetworkSettings.MyRoom);
		projectile.Init(Hero, manaValue, false, this);
		projectile.InitIceCloud(manaValue, Damage);
		projectile.Talent(_boostDmg, _frozwenTalent, _lastHit);

		NetworkServer.Spawn(projectile.gameObject);

		RpcPlayShotSound();
		RpcInit(projectile.gameObject, manaValue);
	}

	[ClientRpc]
	private void RpcInit(GameObject obj, float manaValue)
	{
		if (obj.TryGetComponent<IceCloudProjectile>(out IceCloudProjectile ice))
        {
			ice.Init(Hero, manaValue, false, this);
			ice.InitIceCloud(manaValue, Damage);
		}
	}

	[ClientRpc]
	private void RpcPlayShotSound()
	{
		if (_audioSource != null && _audioClip != null) _audioSource.PlayOneShot(_audioClip);
	}

	public void TalentBoostDmg(bool value)
	{
		_boostDmg = value;
	}

	public void TalentBoostFrozenState(bool value)
	{
		_frozwenTalent = value;
	}

	public override void LoadTargetData(TargetInfo targetInfo)
	{
		if (targetInfo.Points.Count > 0) _mousePos = targetInfo.Points[0];
		if (targetInfo.GetTargets().Count > 0) Targeting.SetTarget((Character)targetInfo.GetTargets()[0]);
	}

	protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
	{
		while (float.IsPositiveInfinity(_mousePos2.x))
		{
			if (GetMouseButton)
			{
				if(Targeting.GetTarget().Character == null) yield return null;
				if (Targeting.GetTarget().Character != null)
				{
					float distance = Vector3.Distance(_hero.transform.position, _mousePos);

					if (distance <= AreaInfo.Radius) _mousePos2 = Targeting.GetTarget().Character.transform.position;

					else
					{
						//Targeting.FindTempTarget();

						//_target = Targeting.GetTarget().character;
						Damage = _baseDamage + _energy.CurrentValue / EnergyPerDamageUnit;
						_mousePos2 = Targeting.GetTarget().Character.transform.position;
					}
				}

				else _mousePos2 = Targeting.GetMousePoint();
			}
			yield return null;
		}        

        TargetInfo targetInfo = new TargetInfo();
		if (Targeting.GetTarget().Character != null) targetInfo.Points.Add(Targeting.GetTarget().Character.Position);
		else if (_mousePos2 != Vector3.positiveInfinity) targetInfo.Points.Add(_mousePos2);
		callbackDataSaved(targetInfo);

        _mousePos = _mousePos2;
        _mousePos2 = Vector3.positiveInfinity;
    }

	protected override IEnumerator CastJob()
	{
		Shoot();
		yield return null;
	}

	protected override void ClearData()
	{
		Targeting.ClearTarget();
		_mousePos = Vector2.positiveInfinity;
		//_enabled = false;
	}

	public void IceCloudsCast()
	{
		AnimStartCastCoroutine();
	}

	public void IceCloudsEnd()
	{
		AnimCastEnded();
	}

	public void CanMoveIceCloud() => Hero.Move.SetCanMove(true);
	public void StopMoveIceCloud() => Hero.Move.SetCanMove(false);
}

