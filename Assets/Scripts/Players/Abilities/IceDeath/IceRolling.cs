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
	//[SerializeField] private RunePlayer _rune;
	[SerializeField] private PlayerLinks _playerLinks;
	[SerializeField] private float _jumprange = 2f;
	[SerializeField] private LayerMask ObstacleLayerMask;
	private Vector2 _jumpPos;
	private bool _canJump = true;
	
	protected override void Cast()
	{
		PayCost();
		if (_playerLinks.RunePlayer.RemoveRune(0.25f, this))
		{
			Jump();
		}
	}

	protected override void Cancel()
	{
		//вроде не было нужды для отмены каста, пока что....
	}

	private void Jump()
	{
		if (_canJump )
		{
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
			
			Vector2 jumpPos = lookDir * actualJumpRange + (Vector2)PlayerMove.transform.position;
			if(CheckObstacleBetween(_playerLinks.Rb.position, jumpPos))
			{
				Debug.Log("Обнаружено препятствие:");
				//прыгать до препятствия
				_playerLinks.Rb.DOMove(_jumpPos, 0.3f * actualJumpRange).OnComplete(AfterJump);
			}
			else
			{
				Mana.Use((actualJumpRange - _jumprange) * 5);
				_playerLinks.Rb.DOMove(jumpPos, 0.3f * actualJumpRange).OnComplete(AfterJump);
			}
		}
	}

	private void AfterJump()
	{
		PlayerMove.CanMove = true;
		_canJump = true;
	}

	private bool CheckObstacleBetween(Vector3 start, Vector3 end)
	{
		//Проверка на наличие препятствия
		Vector2 direction = (end - start).normalized;
		float distance = Vector2.Distance(start, end);

		RaycastHit2D[] hits =
			Physics2D.BoxCastAll(start, new Vector2(1f, 1f), 0f, direction, distance, ObstacleLayerMask);

		foreach (RaycastHit2D hit in hits)
		{
			_jumpPos = hits[0].point - direction;
			return true;
		}

		return false;
	}


}
