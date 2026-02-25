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
	[SerializeField] private LayerMask _groundLayerMask;

	private static readonly int iceRollingStart = Animator.StringToHash("IceRollingStart");
	private static readonly int iceRollingEnd = Animator.StringToHash("IceRollingEnd");

	private AudioSource _audioSource;
	private Vector3 _mousePos = Vector2.positiveInfinity;

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
	private const float EnergyChunkValue = 5f;
	private const float KnockbackDistance = 2f;
	private const float KnockbackDuration = 0.5f;
	private const float AttachedFrozenDuration = 2f;
	private const float DynamicRendererJobTime = 0.2f;
	private const float TargetSearchRadius = 0.5f;
	private const float RayCastDistance = 1000f;
	#endregion

	protected override bool IsCanCast
	{
		get
		{
			if (GetTargetCharacter() != null) return Vector3.Distance(GetTargetCharacter().transform.position, transform.position) <= AreaInfo.Radius;
			else return true;
		}
	}

	private bool IsAllyTarget(IDamageable target) => target.gameObject.layer == LayerMask.NameToLayer("Allies");

	protected override int AnimTriggerCastDelay => 0;
	protected override int AnimTriggerCast => iceRollingStart;

	public void IceRollingCast() => AnimStartCastCoroutine();
	public void IceRollingEnd() => AnimCastEnded();

	private void Start()
	{
		_animator = GetComponent<Animator>();
		_audioSource = GetComponent<AudioSource>();

        _energy = (Energy)_playerLinks.Resources[ResourceType.Energy];
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
			if (hit.collider.TryGetComponent(out Character character))
			{
				if (character != _playerLinks)
                {
					if (!_rollingWithEnemyTalent && character != GetTargetCharacter())
					{
						stopPosition = hit.point - direction;
						characterHit = character;

						Debug.Log("1");
						return true;
					}
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

	private void Jump2()
	{
		Hero.Move.SetCanMove(false);
		_isJump = true;

		_lookDir = (_mousePos - _playerLinks.transform.position).normalized;
		Vector3 startPosition = _playerLinks.transform.position;
		Vector3 rawTargetPos = _mousePos;

		float distanceToClick = Vector3.Distance(startPosition, rawTargetPos);
		float finalRange;
		int extraCells = 0;

		if (distanceToClick <= 2f)
		{
			finalRange = 2f;
			extraCells = 0;
		}
		else if (distanceToClick < 4f)
		{
			finalRange = distanceToClick;
			extraCells = Mathf.CeilToInt(finalRange) - 2;
		}
		else
		{
			finalRange = 4f;
			extraCells = 2;
		}

		float additionalCost = extraCells * 5f;

		//float totalCost = ManaCostRate + additionalCost;

		//if (_energy.CurrentValue < totalCost)
		//{
		//	Debug.Log("Недостаточно энергии для прыжка.");
		//	return;
		//}

		_energy.CmdUse(additionalCost);

		if (_isLastInSeries && GetTargetCharacter() == null && _rollingWithEnemyTalent)
			finalRange *= 1.5f;

		Vector3 jumpPos = startPosition + _lookDir * finalRange;

		Vector3 stopPosition;
		Character characterHit;
		Character characterHitTarget;

		bool hit = CheckObstacleBetween(_playerLinks.transform.position, jumpPos, out stopPosition, out characterHit);
		bool hitTarget = IsTargetInCloseProximity(_playerLinks.transform.position, _lookDir, out characterHitTarget);

		Hero.Move.LookAtPosition(jumpPos);
		float actualDistance = Vector3.Distance(startPosition, stopPosition);

		CmdPush(stopPosition, actualDistance);

		if (_rollingWithEnemyTalent && GetTargetCharacter() != null && hitTarget && characterHitTarget != null)
			CmdPushWithCharacter(stopPosition, characterHitTarget, actualDistance);

		if (_rollingPhysTalent)
		{
			_physicalAttack.TalentRollingPhys(_afterJump, finalRange);
			_afterJump = true;
		}

		if (!_hero.Abilities.SkillQueue.Skills.Contains(this))
		{
			ClearTarget();
			_mousePos = Vector3.positiveInfinity;
			_lookDir = Vector3.zero;
		}
		else
		{
			FindTargetCharacter();
			_mousePos = GetTargetCharacter() != null ? GetTargetCharacter().transform.position : GetMousePoint();
		}
	}

    #region old
 //   private void Jump()
	//{
	//	Hero.Move.CanMove = false;
	//	_isJump = true;
	//	float actualJumpRange = _jumprange;

	//	_lookDir = (_mousePos - _playerLinks.transform.position).normalized;
	//	Vector3 jumpPos = _lookDir * actualJumpRange + _playerLinks.transform.position;
	//	Vector3 stopPosition;
	//	Character characterHit;
	//	if (CheckObstacleBetween(_playerLinks.transform.position, jumpPos, out stopPosition, out characterHit))
	//	{
	//		_jumpCount = 5;
	//		CmdPush(stopPosition, Vector3.Distance(stopPosition, transform.position));
	//	}
	//	else
	//	{
	//		Debug.Log(Vector2.Distance(jumpPos, transform.position) + " Jump " + actualJumpRange);
	//		//if(actualJumpRange)
	//		for (int i = 0; i < 2; i++)
	//		{
	//			_jumpCount += 1f;
	//			actualJumpRange += 1;
	//			Vector3 jumpPos2 = _lookDir * actualJumpRange + _playerLinks.transform.position;
	//			if (_energy.CurrentValue >= 5 && !CheckObstacleBetween(_playerLinks.transform.position, jumpPos2, out stopPosition, out characterHit))
	//			{
	//				_energy.CmdUse(5);
	//				jumpPos = jumpPos2;
	//				//Debug.Log("Additional jump " + i);
	//			}
	//		}
	//		CmdPush(stopPosition, Vector3.Distance(jumpPos, transform.position));

	//		if (_rollingPhysTalent)
	//		{
	//			_physicalAttack.TalentRollingPhys(_afterJump, _jumpCount);
	//			_afterJump = true;
	//		}
	//	}

	//	if (!_hero.Abilities.SkillQueue.Skills.Contains(this))
	//	{
	//		ClearTarget();
	//		_mousePos = Vector3.positiveInfinity;
	//		_lookDir = Vector3.zero;
	//	}
	//}
    #endregion

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
		Vector3 candidatePoint = Vector3.positiveInfinity;

		while (float.IsPositiveInfinity(candidatePoint.x))
		{
			if (GetMouseButton)
			{
				FindTarget(TargetSearchRadius, GetMousePoint());

				if (GetTempTarget() != null && GetTempTarget() is IDamageable damageable)
				{
					if (IsAllyTarget(damageable) || damageable as Character == Hero) ClearTempTarget();
					else candidatePoint = GetTempTarget().Transform.position;
				}

				else candidatePoint = GetMousePoint(_groundLayerMask);

			}

			yield return null;
		}
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Points.Add(candidatePoint);
        callbackDataSaved(targetInfo);
    }

	public override IEnumerator DynamicRendererJob(float time = DynamicRendererJobTime)
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
			_isLastInSeries = _seriesOfStrikes.MakeHit(GetTargetCharacter(), AbilityForm, 1, 0, 0);
			Jump2();
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

	private Vector3 GetMousePoint(LayerMask mask)
	{
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		if (Physics.Raycast(ray, out RaycastHit hit, RayCastDistance, mask)) return hit.point;

		return Vector3.positiveInfinity;
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

		Vector3 startPosPlayer = _playerLinks.transform.position;
		Vector3 startPosTarget = target.transform.position;

		Vector3 direction = (force - startPosPlayer).normalized;
		float distanceBetween = Vector3.Distance(startPosPlayer, startPosTarget);
		Vector3 finalTargetPos = startPosTarget + direction * distanceBetween;

		if (target.TryGetComponent(out MoveComponent moveTarget))
			moveTarget.TargetRpcDoPush(finalTargetPos, _durationOfJump);

		if (_playerLinks.TryGetComponent(out MoveComponent movePlayer))
			movePlayer.TargetRpcDoPush(force, _durationOfJump);

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
		target.Move.SetCanMove(true);
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
		if (target.TryGetComponent(out MoveComponent move)) move.SetCanMove(false);
		if (target.TryGetComponent(out NavMeshAgent agent)) agent.enabled = false;

		if (target.TryGetComponent(out Rigidbody rb))
		{
			rb.isKinematic = true;
			rb.linearVelocity = Vector3.zero;
		}
		target.transform.SetParent(_playerLinks.transform);
	}
}