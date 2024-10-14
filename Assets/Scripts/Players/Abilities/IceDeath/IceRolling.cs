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
	[SerializeField] private float _jumprange = 2f;
	[SerializeField] private float _durationOfJump = 0.3f;

	private Vector2 _mousePos = Vector2.positiveInfinity;
	private Vector2 _jumpPos;
	private Vector2 _lookDir;
	private Energy _energy;

	protected override bool IsCanCast => true;

	private void Start()
	{
		for (int i = 0; i < _playerLinks.Resources.Count; i++)
		{
			if (_playerLinks.Resources[i].Type == ResourceType.Energy)
			{
				_energy = (Energy)_playerLinks.Resources[i];
			}
		}

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
	private void AfterJump()
	{
		//_jumpCount = 4;
		_mousePos = Vector2.positiveInfinity;
		_lookDir = Vector2.zero;
		_jumpPos = Vector2.zero;
	}

	private bool CheckObstacleBetween(Vector3 start, Vector3 end)
	{
		Vector2 direction = (end - start).normalized;
		float distance = Vector2.Distance(start, end);

		RaycastHit2D[] hits =
			Physics2D.BoxCastAll(start, new Vector2(2f, 2f), 0f, direction, distance, _obstacle);

		foreach (RaycastHit2D hit in hits)
		{
			_jumpPos = hits[0].point - direction*1.2f;
			return true;
		}

		return false;
	}

	private void Jump()
	{
		float actualJumpRange = _jumprange * GlobalVariable.cellSize;

		_lookDir = (_mousePos - (Vector2)_playerLinks.transform.position).normalized;
		Vector2 jumpPos = _lookDir * actualJumpRange + (Vector2)_playerLinks.transform.position;
		if (CheckObstacleBetween(_playerLinks.transform.position, jumpPos))
		{
			CmdPush(_jumpPos);
			//прыгать до препятствия
		}
		else
		{
			for (int i = 0; i < 2; i++)
			{
				actualJumpRange += 2;
				Vector2 jumpPos2 = _lookDir * actualJumpRange + (Vector2)_playerLinks.transform.position;
				if (_energy.CurrentValue >= 5 && !CheckObstacleBetween(_playerLinks.transform.position, jumpPos2))
				{
					_energy.CmdUse(5);
					jumpPos = jumpPos2;
				}
			}
			CmdPush(jumpPos);
		}
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
				_mousePos = GetMousePoint();
			}
			yield return null;
		}
	}

	protected override IEnumerator CastJob()
	{
		Jump();
		yield return null;
	}

	protected override void ClearData()
	{
		AfterJump();
	}

	[Command]
	private void CmdPush(Vector2 force)
	{
		_playerLinks.Move.TargetRpcDoMove(force, _durationOfJump);
	}
}
