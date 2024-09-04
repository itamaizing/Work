using Mirror;
using UnityEngine;

public class MoveComponent : NetworkBehaviour
{
	public Vector2 MoveDirection = Vector2.zero;
	
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

		MoveDirection = Vector2.zero;

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
		
		float moveX = Input.GetAxis("Horizontal");
		float moveY = Input.GetAxis("Vertical");
		MoveDirection = new Vector2(moveX, moveY);
		
		if (MoveDirection.magnitude > 1)
		{
			MoveDirection.Normalize();
		}
	}
	
	private void FixedUpdate()
	{
		CmdMove(MoveDirection, _currentSpeed);
	}

	[Command]
	private void CmdMove(Vector2 movement, float moveSpeed)
	{
		_rigidbody.velocity = movement * moveSpeed;
		
		RpcMove(_rigidbody.position, moveSpeed);
	}

	[ClientRpc]
	private void RpcMove(Vector2 position, float moveSpeed)
	{
		if (!isLocalPlayer)
		{
			_rigidbody.position = Vector2.Lerp(_rigidbody.position, position, Time.fixedDeltaTime * moveSpeed);
		}
	}
}