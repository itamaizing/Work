using System.Collections;
using GlobalEvents;
using Players.Abilities.Genjalf;
using Players.Abilities.Genjalf.Shield_Ability;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Mirror;

public class IceRolling : Skill
{
	[Header("Ability properties")]

	[SerializeField] private Character _playerLinks;
	[SerializeField] private PhysicalAttack _physicalAttack;
	[SerializeField] private float _jumprange = 5f;
	[SerializeField] private float _durationOfJump = 0.3f;
	[SerializeField] private AudioClip audioClip;

	private static readonly int iceRollingStart = Animator.StringToHash("IceRollingStart");
	private static readonly int iceRollingEnd = Animator.StringToHash("IceRollingEnd");

	private Animator _animator;
	private AudioSource _audioSource;
	private Vector3 _mousePos = Vector2.positiveInfinity;
	private Vector3 _jumpPos;
	private Vector3 _lookDir;
	private Energy _energy;
	private bool _rollingPhysTalent = false;
	private float _jumpCount = 0;
	private bool _afterJump;
	private float _afterJumpDelay = 1;
	private Character _target;
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
		if(_afterJump)
		{
			TimerDelay();
		}
	}

	private float GetJumpRange() 
	{
		float range = _jumprange;
		float energyCost = 1; 
		for(int i = 0; i < 10; i++)
		{
			if(_energy.CurrentValue >= energyCost)
			{
				range+=0.2F;
				energyCost += 1;
			}
		}

		return range;
	}

	/*private void Jump()
	{
		if (_canJump )
		{
			_enabled = true;
			_isReady = false;
			PlayerMove.CanMove = false;
			_canJump = false;
			float actualJumpRange = _jumprange;

			Vector2 _mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			Vector2 lookDir = (_mousePos - _playerLinks.Rb.position).normalized;
			
			if(Mana.Value >= 10)
			{
				actualJumpRange += 2;
			}
			else if(Mana.Value < 10 && Mana.Value >=5)
			{
				actualJumpRange += 1;
			}
			actualJumpRange *= GlobalVariable.cellSize;
			Vector2 jumpPos = lookDir * actualJumpRange + (Vector2)PlayerMove.transform.position;
			if(CheckObstacleBetween(_playerLinks.Rb.position, jumpPos))
			{
				Debug.Log("Find obstacle:");
				//РїСЂС‹РіР°С‚СЊ РґРѕ РїСЂРµРїСЏС‚СЃС‚РІРёСЏ
				_playerLinks.Rb.DOMove(_jumpPos, _durationOfJump * actualJumpRange / GlobalVariable.cellSize).OnComplete(AfterJump);
			}
			else
			{
				Mana.Use((actualJumpRange - _jumprange) * 5);
				_playerLinks.Rb.DOMove(jumpPos, _durationOfJump * actualJumpRange / GlobalVariable.cellSize).OnComplete(AfterJump);
			}
		}
	}
	//РґРµР»РёРј РЅР° cell size С‡С‚Рѕ Р±С‹ СЃС‡РёС‚Р°Р»РѕСЃСЊ РІСЂРµРјСЏ РЅРµ Р·Р° РѕРґРЅСѓ РµРґРёРЅРёС†Сѓ СЋРЅРёС‚Рё, Р° Р·Р° РЅР°С€Рё, РєР»РµС‚РєРё
	*/
	//private void AfterJump()
	//{
	//	//_jumpCount = 4;
	//	_mousePos = Vector3.positiveInfinity;
	//	_lookDir = Vector3.zero;
	//	_jumpPos = Vector3.zero;
	//}

	private bool CheckObstacleBetween(Vector3 start, Vector3 end, out Vector3 stopPosition)
	{
		Vector3 direction = (end - start).normalized;
		float distance = Vector3.Distance(start, end);

		RaycastHit[] hits = Physics.BoxCastAll(start, new Vector3(0.5f, 0.5f, 0.5f), direction, Quaternion.identity, distance);

		foreach (RaycastHit hit in hits)
		{
			Character hitCharacter = hit.collider.GetComponent<Character>();
			if (hitCharacter != null && hitCharacter != _playerLinks)
			{
				stopPosition = hit.point - direction;
				HandleJumpEnd();
				return true;
			}

			if (((1 << hit.collider.gameObject.layer) & _obstacle) != 0)
			{
				stopPosition = hit.point - direction;
				HandleJumpEnd();
				return true;
			}
		}

		stopPosition = end;
		return false;
	}


	private void Jump()
	{
		Hero.Move.CanMove = false;
		float actualJumpRange = _jumprange;

		_lookDir = (_mousePos - _playerLinks.transform.position).normalized;
		Vector3 jumpPos = _lookDir * actualJumpRange + _playerLinks.transform.position;

		if (CheckObstacleBetween(_playerLinks.transform.position, jumpPos, out Vector3 stopPosition))
		{
			CmdPush(stopPosition);
		}
		else
		{
			for (int i = 0; i < 10; i++)
			{
				_jumpCount += 0.2f;
				actualJumpRange += 0.2f;
				Vector3 jumpPos2 = _lookDir * actualJumpRange + _playerLinks.transform.position;
				if (_energy.CurrentValue >= 5 && !CheckObstacleBetween(_playerLinks.transform.position, jumpPos2, out stopPosition))
				{
					_energy.CmdUse(1);
					jumpPos = jumpPos2;
				}
			}
			CmdPush(jumpPos);

			if (_rollingPhysTalent)
			{
				_physicalAttack.TalentRollingPhys(_afterJump, _jumpCount);
				_afterJump = true;
			}
		}

		_target = null;
		_mousePos = Vector3.positiveInfinity;
		_lookDir = Vector3.zero;
		_jumpPos = Vector3.zero;
		Hero.Move.CanMove = true;
	}

	/*private void NextJump()
	{
		if(_jumpCount > 0)
		{
			Debug.Log("jump " + _jumpCount);
			_jumpCount--;
			Vector2 jumpPos = _lookDir + (Vector2)_playerLinks.transform.position;
			if (CheckObstacleBetween(_playerLinks.transform.position, jumpPos))
			{
				Debug.Log("Обнаружено препятствие:");
				//прыгать до препятствия
				//_playerLinks.Rigidbody2D.DOMove(_jumpPos, _durationOfJump / GlobalVariable.cellSize).SetEase(Ease.Linear).OnComplete(AfterJump);
			}
			else
			{
				//_playerLinks.Rigidbody2D.DOMove(jumpPos, _durationOfJump / GlobalVariable.cellSize).SetEase(Ease.Linear).OnComplete(NextJump);
			}
		}
		else
		{
			//AfterJump();
		}
	}*/

	protected override IEnumerator PrepareJob()
	{
		while (float.IsPositiveInfinity(_mousePos.x))
		{
			if (GetMouseButton)
			{
				if (GetTarget().isCharater)
				{
					float distance = Vector3.Distance(_hero.transform.position, _mousePos);

					if (distance <= Radius) _mousePos = GetTarget().character.transform.position;

					else
					{
						_target = GetTarget().character;
						_mousePos = _target.transform.position;
					}
				}

				else _mousePos = GetTarget().Position;
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
		Jump();
		yield return null;
	}

	protected override void ClearData()
	{
		//AfterJump();
	}

	[Command]
	private void CmdPush(Vector3 force)
	{
		RpcPlayShotSound();
		_playerLinks.Move.TargetRpcDoMove(force, _durationOfJump);
		StartCoroutine(WaitForJumpEnd());
	}

	public void IceRollingCast()
	{
		AnimStartCastCoroutine();
	}

	public void IceRollingEnd()
	{
		AnimCastEnded();
	}

	public void TalentRollingPhys(bool value)
	{
		_rollingPhysTalent = value;
	}

	private void HandleJumpEnd()
	{
		if (_animator != null)
		{
			_animator.ResetTrigger(iceRollingStart);
			_animator.SetTrigger(iceRollingEnd);
		}
	}

	private void TimerDelay()
	{
		_afterJumpDelay -= Time.deltaTime;
		if( _afterJumpDelay < 0 )
		{
			_afterJump = false;
			_physicalAttack.TalentRollingPhys(_afterJump, 0);
		}
	}

	private IEnumerator WaitForJumpEnd()
	{
		yield return new WaitForSeconds(_durationOfJump);
		RpcOnJumpEnd();
	}

	[ClientRpc]
	private void RpcPlayShotSound()
	{
		if (_audioSource != null && audioClip != null) _audioSource.PlayOneShot(audioClip);
	}

	[ClientRpc]
	private void RpcOnJumpEnd()
	{
		HandleJumpEnd();
	}
}
