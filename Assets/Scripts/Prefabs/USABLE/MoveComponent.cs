using Mirror;
using UnityEngine;

public class MoveComponent : NetworkBehaviour
{
	private Vector2 _offset = Vector2.zero; // new
	
	private Rigidbody2D _rigidbody;
	
	private Vector2 target;

	public bool CanMove;
	public bool IsMoving;
	public bool IsSelect;
	public Vector2 MoveDirection;

	private float _defaultSpeed;
	private float _currentSpeed;

	private bool isInitialize = false;

	public void SetOffset(Vector2 offset) // new
	{
		_offset = offset;
	}

	public void Initialize(float speed, Rigidbody2D rb)
	{
		_defaultSpeed = speed;

		_rigidbody = rb;
		_rigidbody.isKinematic = false;
		
		SetDefaultSpeed();

		MoveDirection = Vector2.down;

		CanMove = true;
		isInitialize = true;
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
	
	private void SetMoveDirection()
	{
		if (GetComponent<HeroComponent>() == null) return;

		Vector2 move = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

		if (move == Vector2.zero) return;

		target = transform.position + (Vector3)move * _currentSpeed;
		
	}
}