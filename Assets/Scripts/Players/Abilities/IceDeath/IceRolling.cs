using System.Collections;
using GlobalEvents;
using Players.Abilities.Genjalf;
using Players.Abilities.Genjalf.Shield_Ability;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Mirror;
using UnityEngine.AI;

public class IceRolling : Skill
{
	[Header("Ability properties")]

	[SerializeField] private Character _playerLinks;
	[SerializeField] private PhysicalAttack _physicalAttack;
	[SerializeField] private SeriesOfStrikes seriesOfStrikes;
	[SerializeField] private float _jumprange = 5f;
	[SerializeField] private float _durationOfJump = 0.3f;
	[SerializeField] private AudioClip audioClip;
	[SerializeField] private LayerMask _groundLayer;

	private static readonly int iceRollingStart = Animator.StringToHash("IceRollingStart");
	private static readonly int iceRollingEnd = Animator.StringToHash("IceRollingEnd");

	private AudioSource _audioSource;
	private Vector3 _mousePos = Vector2.positiveInfinity;
	private Vector3 _jumpPos;
	private Vector3 _lookDir;
	private Energy _energy;

	private bool _rollingPhysTalent = false;
	private bool _rollingWithEnemyTalent = false;

	private bool _isLastInSeries = false;

	private float _jumpCount = 0;
	private bool _afterJump;
	private float _afterJumpDelay = 1;
	private Character _target;
	private Animator _animator;
	//private float TEMPFLOAT = 1;

	protected override bool IsCanCast
	{
		get
		{
			if (_target != null) return Vector3.Distance(_target.transform.position, transform.position) <= Radius;
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
			{
				_energy = (Energy)_playerLinks.Resources[i];
			}
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
		for (int i = 0; i < 10; i++)
		{
			if (_energy.CurrentValue >= energyCost)
			{
				range += 0.2F;
				energyCost += 1;
			}
		}

		return range;
	}

	private bool CheckObstacleBetween(Vector3 start, Vector3 end, out Vector3 stopPosition, out Character characterHit)
	{
		Vector3 direction = (end - start).normalized;
		float distance = Vector3.Distance(start, end);

		RaycastHit[] hits = Physics.BoxCastAll(start, new Vector3(0.05f, 0.05f, 0.05f), direction, Quaternion.identity, distance);

		stopPosition = end;
		characterHit = null;

		foreach (RaycastHit hit in hits)
		{
			if (hit.collider.TryGetComponent(out Character character) && character != _playerLinks)
			{
				if (!_rollingWithEnemyTalent)
				{
					stopPosition = hit.point - direction;
					characterHit = character;
					return true;
				}

				else if (_rollingWithEnemyTalent && _target != null)
				{
					characterHit = character;
					return true;
				}
			}

			if (((1 << hit.collider.gameObject.layer) & _obstacle) != 0)
			{
				stopPosition = hit.point - direction;
				return true;
			}
		}

		return false;
	}

	private void Jump()
	{
		Hero.Move.CanMove = false;
		_lookDir = (_mousePos - _playerLinks.transform.position).normalized;
		_playerLinks.Move.LookAtPosition(_lookDir);

		float actualJumpRange = _jumprange;
		Vector3 jumpPos = _playerLinks.transform.position + _lookDir * actualJumpRange;
		Vector3 stopPosition;
		Character characterHit;

		for (int i = 0; i < 10; i++)
		{
			float extendedRange = _jumprange + (i + 1) * 0.2f;
			Vector3 tryPos = _playerLinks.transform.position + _lookDir * extendedRange;

			if (_energy.CurrentValue >= 5 && !CheckObstacleBetween(_playerLinks.transform.position, tryPos, out _, out _))
			{
				_energy.CmdUse(5);
				_jumpCount += 0.2f;
				actualJumpRange = extendedRange;
				jumpPos = tryPos;
			}
		}

		bool hit = CheckObstacleBetween(_playerLinks.transform.position, jumpPos, out stopPosition, out characterHit);

		if (hit && characterHit != null)
		{
			if (_rollingWithEnemyTalent && characterHit == _target)
			{
				CmdPushWithCharacter(stopPosition, characterHit);
				KnockbackCharactersAlongPath(_playerLinks.transform.position, stopPosition);
			}

			else CmdPush(stopPosition);

			return;
		}

		if (_rollingWithEnemyTalent) KnockbackCharactersAlongPath(_playerLinks.transform.position, jumpPos);

		CmdPush(jumpPos);

		if (_rollingPhysTalent)
		{
			_physicalAttack.TalentRollingPhys(_afterJump, _jumpCount);
			_afterJump = true;
		}

		_mousePos = Vector3.positiveInfinity;
		_lookDir = Vector3.zero;
		_jumpPos = Vector3.zero;
	}


	private void KnockbackCharactersAlongPath(Vector3 start, Vector3 end)
	{
		Vector3 direction = (end - start).normalized;
		float distance = Vector3.Distance(start, end);
		RaycastHit[] hits = Physics.BoxCastAll(start, new Vector3(0.3f, 0.3f, 0.3f), direction, Quaternion.identity, distance, _targetsLayers);

		foreach (var hit in hits)
		{
			if (hit.collider.TryGetComponent(out Character character) && character != _playerLinks && character != _target && ((1 << character.gameObject.layer) & _targetsLayers) != 0)
			{
				Vector3 right = Vector3.Cross(direction, Vector3.up);
				Vector3 pushDir = right * UnityEngine.Random.Range(-1f, 1f);
				CmdKnockback(character, character.transform.position + pushDir);
			}
		}
	}

	protected override IEnumerator PrepareJob()
	{
		while (float.IsPositiveInfinity(_mousePos.x))
        {
			if (GetMouseButton)
			{
				_target = GetRaycastTarget();

				if (_target != null) _mousePos = _target.transform.position;

				else _mousePos = GetMousePoint();
			}

			yield return null;
		}
	}

	protected override IEnumerator DynamicRendererJob(float time = 0.2f)
	{
		while (true)
		{
			yield return new WaitForSeconds(time);
			_skillRender.SetSizeBox(1, GetJumpRange());
		}
	}

	protected override IEnumerator CastJob()
	{
		_isLastInSeries = seriesOfStrikes.MakeHit(_target, AbilityForm.Physical, 1, 0, 0);
		Jump();
		yield return null;
	}

	protected override void ClearData() { }

	[Command]
	private void CmdPush(Vector3 force)
	{
		RpcPlayShotSound();
		force.y = 1;
		_playerLinks.Move.TargetRpcDoMove(force, _durationOfJump);
		StartCoroutine(WaitForJumpEnd());
	}

	[Command]
	private void CmdPushWithCharacter(Vector3 force, Character target)
	{
		RpcPlayShotSound();

		if (target.TryGetComponent(out MoveComponent move))
		{
			move.CanMove = false;
			target.transform.SetParent(_playerLinks.transform);
			RpcAttachTarget(target);
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
			if (target.connectionToClient != null) move.TargetRpcDoMove(force, 0.1f);
			else move.RpcDoMove(force, 0.1f);
		}
	}

	private void HandleJumpEnd()
	{
		if (_animator != null)
		{
			_animator.ResetTrigger(iceRollingStart);
			_animator.SetTrigger(iceRollingEnd);
		}

		_playerLinks.Move.StopLookAt();
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
			_afterJump = false;
			_physicalAttack.TalentRollingPhys(_afterJump, 0);
		}
	}

	[ClientRpc]
	private void RpcReleaseTarget()
	{
		if (_target != null)
		{
			_target.Move.CanMove = true;
			_target.transform.SetParent(null);

			if (_target.TryGetComponent(out NavMeshAgent agent))
			{
				agent.enabled = true;
				agent.Warp(_target.transform.position);
			}

			if (_target.TryGetComponent(out Rigidbody rigidbody)) rigidbody.isKinematic = false;
		}
	}

	private IEnumerator WaitForJumpEnd()
	{
		yield return new WaitForSeconds(_durationOfJump);
		RpcOnJumpEnd();
		//_target.CharacterState.AddState(States.Frozen, 2f, 0f, _playerLinks.gameObject, Name);
		RpcReleaseTarget();
	}

	[ClientRpc] private void RpcPlayShotSound() => _audioSource?.PlayOneShot(audioClip);
	[ClientRpc] private void RpcOnJumpEnd() => HandleJumpEnd();

	[ClientRpc]
	private void RpcAttachTarget(Character target)
	{
		if (target.TryGetComponent(out MoveComponent move)) move.CanMove = false;
		if (target.TryGetComponent(out NavMeshAgent agent)) agent.enabled = false;

		if (target.TryGetComponent(out Rigidbody rb))
		{
			rb.isKinematic = true;
			rb.velocity = Vector3.zero;
		}

		target.transform.SetParent(_playerLinks.transform);
	}
}