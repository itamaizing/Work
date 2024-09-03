using Mirror;
using UnityEngine;

public class MoveComponent : NetworkBehaviour
{
	public Vector2 MoveDirection;
	
	public bool CanMove = false;
	public bool IsMoving = false;
	public bool IsSelect = false;
	
	private Rigidbody2D _rigidbody;
	
	private Vector2 _offset = Vector2.zero;
	
	private bool _isHero = false;
	
	private float _defaultSpeed;
	private float _currentSpeed;

	public void SetOffset(Vector2 offset)
	{
		_offset = offset;
	}

	public void Initialize(float speed, Rigidbody2D rb , bool isHero = false)
	{
		_defaultSpeed = speed;

		_rigidbody = rb;
		_rigidbody.isKinematic = false;
		
		SetDefaultSpeed();

		MoveDirection = Vector2.down;

		CanMove = true;
		_isHero = isHero;
	}

	public void ChangeMoveSpeed(float value)
	{
		_currentSpeed *= value;
	}
	public void SetMoveSpeed(float speed)
	{
		_currentSpeed = speed;
	}
	public void SetDefaultSpeed()
	{
		_currentSpeed = _defaultSpeed;
	}
	
	void Update()
	{
		if (!isLocalPlayer || !CanMove || !IsSelect)
		{
			return;
		}
		
		HandleKeyboardInput();
	}

	[Client]
	private void HandleKeyboardInput()
	{
		if (!_isHero) return;

		float moveX = Input.GetAxis("Horizontal");
		float moveY = Input.GetAxis("Vertical");
		MoveDirection = new Vector2(moveX, moveY).normalized;

		if (MoveDirection != Vector2.zero)
		{
			CmdMove(MoveDirection, _currentSpeed);
		}
		else
		{
			CmdStopMovement();
		}
	}

	private void CmdMove(Vector2 moveDirection, float speed)
	{
		_rigidbody.velocity = moveDirection * speed;
	}

	private void CmdStopMovement()
	{
		_rigidbody.velocity = Vector2.zero;
	}
}