using System.Collections;
using UnityEngine;
using Mirror;
using UnityEngine.AI;
using System;

public class IceRolling : Skill
{
	[Header("Ability properties")]

	[SerializeField] private Character _playerLinks;
	[SerializeField] private PhysicalAttack _physicalAttack;
	[SerializeField] private SeriesOfStrikes _seriesOfStrikes;
	[SerializeField] private float _jumprange = 2f;
	[SerializeField] private float _durationOfJumpPerCell = 0.3f;
	[SerializeField] private AudioClip _audioClip;
	//[SerializeField] private LayerMask _groundLayer;

	private static readonly int iceRollingStart = Animator.StringToHash("IceRollingStart");
	private static readonly int iceRollingEnd = Animator.StringToHash("IceRollingEnd");

	private AudioSource _audioSource;
	private Vector3 _mousePos = Vector2.positiveInfinity;
	private Vector3 _mousePos2 = Vector2.positiveInfinity;

	//private Vector3 _jumpPos;
	private Vector3 _lookDir;
	private Energy _energy;

	private bool _rollingPhysTalent = false;
	private bool _rollingWithEnemyTalent = false;

	private bool _isLastInSeries = false;
	private bool _isJump = false;

	private float _durationOfJump;
	private float _jumpCount = 0;
	private bool _afterJump;
	private float _afterJumpDelay = 1;
	//private Character _target;
	private Character _attachedTarget;
	private Animator _animator;

	#region Constants
	private const float BoxCastSize = 0.05f;
	private const float ObstaclePushBackMultiplier = 1.2f;
	private const float RollingWithEnemyTalentBaseRange = 4f;
	private const float RollingWithoutEnemyTalentBaseRange = 2f;
	private const float EnergyChunkValue = 5f;
	private const float KnockbackDistance = 2f;
	private const float KnockbackDuration = 0.05f;
	private const float AttachedFrozenDuration = 2f;
	private const float DynamicRendererJobTime = 0.2f;
	#endregion

	protected override bool IsCanCast
	{
		get
		{
			if (GetTargetCharacter() != null) return Vector3.Distance(GetTargetCharacter().transform.position, transform.position) <= Radius;
			else return true;
		}
	}

	protected override int AnimTriggerCastDelay => 0;
	protected override int AnimTriggerCast => iceRollingStart;

	public void IceRollingCast() => AnimStartCastCoroutine();
	public void IceRollingEnd() => AnimCastEnded();

	private void Start()
	{
		_animator = GetComponent<Animator>();
		_audioSource = GetComponent<AudioSource>();

		for (int i = 0; i < _playerLinks.Resources.Count; i++)
		{
			if (_playerLinks.Resources[i].Type == ResourceType.Energy)
				_energy = (Energy)_playerLinks.Resources[i];
		}
	}

	private void Update()
	{
		if (_afterJump)
		{
			TimerDelay();
		}
	}

	private float GetJumpRange()
	{
		float range = _jumprange;
		float energyCost = 1;
		for (int i = 0; i < 2; i++)
		{
			if (_energy.CurrentValue >= energyCost)
			{
				range += 1;
				energyCost += EnergyChunkValue;
			}
		}

		return range;
	}

	private bool CheckObstacleBetween(Vector3 start, Vector3 end, out Vector3 stopPosition, out Character characterHit)
	{
		Vector3 direction = (end - start).normalized;
		float distance = Vector3.Distance(start, end);

		RaycastHit[] hits = Physics.BoxCastAll(start, new Vector3(BoxCastSize, BoxCastSize, BoxCastSize), direction, Quaternion.identity, distance, _obstacle);

		stopPosition = end;
		characterHit = null;

		foreach (RaycastHit hit in hits)
		{
			if (hit.collider.TryGetComponent(out Character character) && character != _playerLinks)
			{
				if (!_rollingWithEnemyTalent && character != GetTargetCharacter())
				{
					stopPosition = hit.point - direction;
					characterHit = character;
					return true;
				}
			}
			/*int objectLayer = hit.collider.gameObject.layer;
			int objectLayerMask = 1 << objectLayer;
			if ((_obstacle & objectLayerMask) > 0 )
			{
				stopPosition = hits[0].point - direction * 1.2f;
				return true;
			}*/
			//stopPosition = hits[0].point - direction * 1.2f;
			if (hit.collider.transform.root != transform.root)
			{
				Debug.Log(hit.collider.gameObject.name + " Stop point " + stopPosition);
				stopPosition = hit.point - direction * ObstaclePushBackMultiplier;
				return true;
			}
		}

		return false;
	}

	private bool IsTargetInCloseProximity(Vector3 start, Vector3 direction, out Character characterHit)
	{
		Ray ray = new Ray(start, direction);
		RaycastHit[] hits = Physics.RaycastAll(ray, 1);

		characterHit = null;

		foreach (RaycastHit hit in hits)
		{
			if (hit.collider.TryGetComponent(out Character character) && character == characterHit && character != _playerLinks)
			{
				characterHit = character;
				return true;
			}
		}

		return false;
	}

	//private void Jump2()
	//{
	//	Hero.Move.CanMove = false;
	//	_isJump = true;

	//	_lookDir = (_mousePos - _playerLinks.transform.position).normalized;
	//	float baseRange = _rollingWithEnemyTalent ? RollingWithEnemyTalentBaseRange : RollingWithoutEnemyTalentBaseRange;
	//	float maxEnergy = Mathf.Min(_energy.CurrentValue, 10f);
	//	int energyBlocks = Mathf.FloorToInt(maxEnergy / EnergyChunkValue);
	//	float bonusRange = _rollingWithEnemyTalent ? energyBlocks * RollingWithoutEnemyTalentBaseRange : energyBlocks * 1f;

	//	Vector3 startPosition = _playerLinks.transform.position;
	//	Vector3 rawTargetPos = _mousePos;

	//	float distanceToClick = Vector3.Distance(startPosition, rawTargetPos);
	//	float finalRange = Mathf.Min(distanceToClick, GetJumpRange());

	//	float energyMy = finalRange / 5;

	//	if (_isLastInSeries && GetTargetCharacter() == null && _rollingWithEnemyTalent) finalRange *= 1.5f;

	//	Vector3 jumpPos = startPosition + _lookDir * finalRange;

	//	//float energyUsed = energyBlocks * 5f;
	//	//if (energyUsed > 0) _energy.CmdUse(energyUsed);
	//	_energy.CmdUse(energyMy);

	//	Vector3 stopPosition;
	//	Character characterHit;
	//	Character characterHitTarget;

	//	bool hit = CheckObstacleBetween(_playerLinks.transform.position, jumpPos, out stopPosition, out characterHit);
	//	bool hitTarget = IsTargetInCloseProximity(_playerLinks.transform.position, _lookDir, out characterHitTarget);

	//	Hero.Move.LookAtPosition(jumpPos);
	//	float actualDistance = Vector3.Distance(startPosition, stopPosition);

	//	//if (hit && characterHit != null) CmdPush(stopPosition, actualDistance);
	//	//else
	//	CmdPush(stopPosition, actualDistance);

	//	if (_rollingWithEnemyTalent && GetTargetCharacter() != null && hitTarget && characterHitTarget != null) CmdPushWithCharacter(stopPosition, characterHitTarget, actualDistance);

	//	if (_rollingPhysTalent)
	//	{
	//		_physicalAttack.TalentRollingPhys(_afterJump, finalRange);
	//		_afterJump = true;
	//	}


	//	if (!_hero.Abilities.SkillQueue.Skills.Contains(this))
	//	{
	//		ClearTarget();
	//		//_target = null;
	//		_mousePos = Vector3.positiveInfinity;
	//		_lookDir = Vector3.zero;
	//		//_jumpPos = Vector3.zero;
	//	}
	//	else
	//	{
	//		FindTargetCharacter();
	//		//_target = GetTarget().character;
	//		_mousePos = GetTargetCharacter() != null ? GetTargetCharacter().transform.position : GetMousePoint();
	//	}
		
	//}

	private void Jump()
	{
		Hero.Move.CanMove = false;
		_isJump = true;
		float actualJumpRange = _jumprange;

		_lookDir = (_mousePos - _playerLinks.transform.position).normalized;
		Vector3 jumpPos = _lookDir * actualJumpRange + _playerLinks.transform.position;
		Vector3 stopPosition;
		Character characterHit;
		if (CheckObstacleBetween(_playerLinks.transform.position, jumpPos, out stopPosition, out characterHit))
		{
			_jumpCount = 5;
			CmdPush(stopPosition, Vector3.Distance(stopPosition, transform.position));
		}
		else
		{
			Debug.Log(Vector2.Distance(jumpPos, transform.position) + " Jump " + actualJumpRange);
			//if(actualJumpRange)
			for (int i = 0; i < 2; i++)
			{
				_jumpCount += 1f;
				actualJumpRange += 1;
				Vector3 jumpPos2 = _lookDir * actualJumpRange + _playerLinks.transform.position;
				if (_energy.CurrentValue >= 5 && !CheckObstacleBetween(_playerLinks.transform.position, jumpPos2, out stopPosition, out characterHit))
				{
					_energy.CmdUse(5);
					jumpPos = jumpPos2;
					//Debug.Log("Additional jump " + i);
				}
			}
			CmdPush(stopPosition, Vector3.Distance(jumpPos, transform.position));

			if (_rollingPhysTalent)
			{
				_physicalAttack.TalentRollingPhys(_afterJump, _jumpCount);
				_afterJump = true;
			}
		}

		if (!_hero.Abilities.SkillQueue.Skills.Contains(this))
		{
			ClearTarget();
			_mousePos = Vector3.positiveInfinity;
			_lookDir = Vector3.zero;
		}
	}

	public override void LoadTargetData(TargetInfo targetInfo)
	{
		if (targetInfo != null)
		{
			/*if (targetInfo.GetTargets() != null && targetInfo.GetTargets().Count > 0)
			{
				if (targetInfo.GetTargets()[0] is Character character)
					SetTarget(character);
				else SetTarget(ClosedTarget());
			}*/
			if(targetInfo.Points.Count > 0)
			{
				_mousePos = targetInfo.Points[0];
				//targetInfo.Points.RemoveAt(0);
			}
		}
		
	}

	protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
	{
		while (float.IsPositiveInfinity(_mousePos2.x))
		{
			if (GetMouseButton)
			{
				FindTarget();
				//_target = GetTarget().character;
				_mousePos2 = GetTarget() != null ? GetTarget().Transform.position : GetMousePoint();
                
            }
			yield return null;
		}
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Points.Add(_mousePos2);
        callbackDataSaved(targetInfo);
		_mousePos = _mousePos2;
		_mousePos2 = Vector3.positiveInfinity;
    }

	protected override IEnumerator DynamicRendererJob(float time = DynamicRendererJobTime)
	{
		while (IsPreparing)
		{
			_skillRender.SetSizeBox(1, GetJumpRange());
			yield return new WaitForSeconds(time);
		}
	}

	protected override IEnumerator CastJob()
	{
		if (!float.IsInfinity(_mousePos.x))
        {
			_isLastInSeries = _seriesOfStrikes.MakeHit(GetTargetCharacter(), AbilityForm.Physical, 1, 0, 0);
			Jump();
			yield return null;
		}
	}

	protected override void ClearData()
	{
		if (!_hero.Abilities.SkillQueue.Skills.Contains(this))
		{
			ClearTarget();
			//_target = null;
			_mousePos = Vector3.positiveInfinity;
		}
		/*else
		{
			_mousePos = GetMousePoint();
		}*/
		_isJump = false;
		Hero.Move.StopLookAt();
	}

	private void HandleJumpEnd()
	{
		if (_animator != null)
		{
			_animator.ResetTrigger(iceRollingStart);
			_animator.SetTrigger(iceRollingEnd);
		}
	}

	#region Talent
	public void TalentRollingPhys(bool value) => _rollingPhysTalent = value;
	public void RollingWithEnemyTalentActive(bool value) => _rollingWithEnemyTalent = value;
	#endregion

	private void TimerDelay()
	{
		_afterJumpDelay -= Time.deltaTime;
		if (_afterJumpDelay < 0)
		{
			_afterJumpDelay = 1;
			_afterJump = false;
			_physicalAttack.TalentRollingPhys(_afterJump, 0);
		}
	}

	private IEnumerator WaitForJumpEnd()
	{
		yield return new WaitForSeconds(_durationOfJump);
		RpcOnJumpEnd();

		if (_attachedTarget)
        {
			_attachedTarget.CharacterState.AddState(States.Frozen, AttachedFrozenDuration, 0f, _playerLinks.gameObject, Name);
			_attachedTarget.transform.SetParent(null);
			RpcReleaseTarget(_attachedTarget);
			_attachedTarget = null;
		}
	}

	private void OnCollisionEnter(Collision collision)
    {
		if (_rollingWithEnemyTalent && _isJump && collision.collider.TryGetComponent(out Character character) && character != _playerLinks && ((1 << character.gameObject.layer) & _targetsLayers) != 0)
		{
			Vector3 pushDir = (character.transform.position - _playerLinks.transform.position).normalized;
			pushDir.y = 0;
			Vector3 pushTarget = character.transform.position + pushDir * KnockbackDistance;
			CmdKnockback(character, pushTarget);
		}
	}

	[Command]
	private void CmdPush(Vector3 force, float finalRange)
	{
		RpcPlayShotSound();
		_durationOfJump = finalRange * _durationOfJumpPerCell;
		force.y = 1;
		_playerLinks.Move.TargetRpcDoMove(force, _durationOfJump);
		StartCoroutine(WaitForJumpEnd());
	}

	[Command]
	private void CmdPushWithCharacter(Vector3 force, Character target, float finalRange)
	{
		RpcPlayShotSound();
		_durationOfJump = finalRange * _durationOfJumpPerCell;

		if (target.TryGetComponent(out MoveComponent move))
		{
			move.CanMove = false;
			_attachedTarget = target;
			_attachedTarget.transform.SetParent(_playerLinks.transform);
			RpcAttachTarget(_attachedTarget);
		}

		force.y = 1;
		_playerLinks.Move.TargetRpcDoMove(force, _durationOfJump);

		StartCoroutine(WaitForJumpEnd());
	}

	[Command]
	private void CmdKnockback(Character target, Vector3 force)
	{
		if (target.TryGetComponent(out MoveComponent move))
		{
			
			if (target.connectionToClient != null) move.TargetRpcDoMove(force, KnockbackDuration);
			else move.RpcDoMove(force, KnockbackDuration);
		}
	}

	[ClientRpc]
	private void RpcReleaseTarget(Character target)
	{
		target.Move.CanMove = true;
		target.transform.SetParent(null);

		if (target.TryGetComponent(out NavMeshAgent agent))
		{
			agent.enabled = true;
			agent.Warp(GetTargetCharacter().transform.position);
		}

		if (target.TryGetComponent(out Rigidbody rigidbody)) rigidbody.isKinematic = false;
	}

	[ClientRpc] private void RpcPlayShotSound() => _audioSource?.PlayOneShot(_audioClip);
	[ClientRpc] private void RpcOnJumpEnd() => HandleJumpEnd();

	[ClientRpc]
	private void RpcAttachTarget(Character target)
	{
		if (target.TryGetComponent(out MoveComponent move)) move.CanMove = false;
		if (target.TryGetComponent(out NavMeshAgent agent)) agent.enabled = false;

		if (target.TryGetComponent(out Rigidbody rb))
		{
			rb.isKinematic = true;
			rb.linearVelocity = Vector3.zero;
		}
		target.transform.SetParent(_playerLinks.transform);
	}
}