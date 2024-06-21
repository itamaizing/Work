using System.Collections;
using GlobalEvents;
using Players.Abilities.Genjalf;
using Players.Abilities.Genjalf.Shield_Ability;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class IceRolling : Ability
{
	[Header("Ability properties")]
	//[SerializeField] private Rigidbody2D _rb;
	//[SerializeField] private RuneComponent _rune;
	[SerializeField] private Character _playerLinks;
	[SerializeField] private float _jumprange = 2f;
	[SerializeField] private float _durationOfJump = 0.3f;
	[SerializeField] private LayerMask _obstacleLayerMask;
	//[SerializeField] private GameObject _croosFire;

	private Vector2 _mousePos;
	private Vector2 _jumpPos;
	private Vector2 _lookDir;
	//private float _angle;
	private bool _canJump = true;
	private bool _enabled = false;
	[SerializeField] private int _jumpCount = 4;

	private void Update()
	{
		if(!_enabled) return;

		//_mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		//Vector2 lookDir = _mousePos - _playerLinks.Rb.position;
		//_angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
		//_croosFire.transform.rotation = Quaternion.Euler(_croosFire.transform.rotation.x, _croosFire.transform.rotation.y, _angle);

		if (Input.GetMouseButtonDown(0))
		{
			//PayCost();
			if (_playerLinks.RuneComponent.RemoveRune(0.25f, this))
			{
				Jump();
			}
			else
			{
				Cancel();
			}
		}
		if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
		{
			Cancel();
		}
	}
	protected override void Cast()
	{
		_enabled = true;		
	}

	protected override void Cancel()
	{
		_enabled = false;
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
		Debug.Log("can jump");
		PlayerMove.CanMove = true;
		_canJump = true;
		//_enabled = false;
		_isReady = true;
		_jumpCount = 4;
	}

	private bool CheckObstacleBetween(Vector3 start, Vector3 end)
	{
		//РџСЂРѕРІРµСЂРєР° РЅР° РЅР°Р»РёС‡РёРµ РїСЂРµРїСЏС‚СЃС‚РІРёСЏ
		Vector2 direction = (end - start).normalized;
		float distance = Vector2.Distance(start, end);

		RaycastHit2D[] hits =
			Physics2D.BoxCastAll(start, new Vector2(2f, 2f), 0f, direction, distance, _obstacleLayerMask);

		foreach (RaycastHit2D hit in hits)
		{
			_jumpPos = hits[0].point - direction*1.2f;
			return true;
		}

		return false;
	}

	private void Jump()
	{
		if (_canJump)
		{
			_enabled = true;
			_isReady = false;
			PlayerMove.CanMove = false;
			_canJump = false;
			float actualJumpRange = _jumprange;

			_mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			_lookDir = (_mousePos - _playerLinks.Rb.position).normalized;
			Debug.Log("jump");
			actualJumpRange *= GlobalVariable.cellSize;
			Vector2 jumpPos = _lookDir * actualJumpRange + (Vector2)PlayerMove.transform.position;
			if (CheckObstacleBetween(_playerLinks.Rb.position, jumpPos))
			{
				Debug.Log("Обнаружено препятствие:");
				//прыгать до препятствия
				_playerLinks.Rb.DOMove(_jumpPos, _durationOfJump * actualJumpRange / GlobalVariable.cellSize).SetEase(Ease.Linear).OnComplete(AfterJump);
			}
			else
			{
				_playerLinks.Rb.DOMove(jumpPos, _durationOfJump * actualJumpRange / GlobalVariable.cellSize).SetEase(Ease.Linear).OnComplete(NextJump);
			}
		}
	}

	private void NextJump()
	{
		if(_jumpCount > 0 && Mana.Use(5))
		{
			Debug.Log("jump " + _jumpCount);
			_jumpCount--;
			Vector2 jumpPos = _lookDir + (Vector2)PlayerMove.transform.position;
			if (CheckObstacleBetween(_playerLinks.Rb.position, jumpPos))
			{
				Debug.Log("Обнаружено препятствие:");
				//прыгать до препятствия
				_playerLinks.Rb.DOMove(_jumpPos, _durationOfJump / GlobalVariable.cellSize).SetEase(Ease.Linear).OnComplete(AfterJump);
			}
			else
			{
				_playerLinks.Rb.DOMove(jumpPos, _durationOfJump / GlobalVariable.cellSize).SetEase(Ease.Linear).OnComplete(NextJump);
			}
		}
		else
		{
			AfterJump();
		}
	}
}
