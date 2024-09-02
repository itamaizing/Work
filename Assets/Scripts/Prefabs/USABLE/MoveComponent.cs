using Mirror;
using UnityEngine;

public class MoveComponent : NetworkBehaviour
{
	public bool CanMove;
	public bool IsMoving;
	public bool IsSelect;
	public bool IsHero = false;
	public Vector2 MoveDirection;

	private Vector2 _offset = Vector2.zero; // new
	[SyncVar]
	private Vector3 _targetPosition;
	
	private Rigidbody2D _rigidbody;
	
	private Vector2 target;


	private float _defaultSpeed;
	private float _currentSpeed;

	private bool _isInitialize = false;

	public float CurrentSpeed { get => _currentSpeed; }
	public float DefaultSpeed { get => _defaultSpeed; }

	public void SetOffset(Vector2 offset) // new
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
		IsHero = isHero;
		_isInitialize = true;
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

	[ClientCallback]
	void Update()
	{
		if (!isOwned || !CanMove || !IsSelect)
		{
			return;
		}
        
		HandleMouseInput();
		HandleKeyboardInput();
	}

	[Client]
	private void HandleMouseInput()
	{
		if (Input.GetKeyDown(KeyCode.Mouse1))
		{
			Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			mousePosition.z = transform.position.z;
			
			CmdSetTargetPosition(mousePosition);
		}
	}

	[Client]
	private void HandleKeyboardInput()
	{
		if (!IsHero) return;

		MoveDirection = new Vector2(
			Input.GetKey(KeyCode.A) ? -1 : Input.GetKey(KeyCode.D) ? 1 : 0,
			Input.GetKey(KeyCode.S) ? -1 : Input.GetKey(KeyCode.W) ? 1 : 0
		);

		if (MoveDirection != Vector2.zero)
		{
			Vector3 targetPosition = transform.position + (Vector3)MoveDirection * (_currentSpeed * Time.deltaTime);
			CmdSetTargetPosition(targetPosition);
		}
	}

	[Command]
	private void CmdSetTargetPosition(Vector3 targetPosition)
	{
		_targetPosition = targetPosition;
	}

	void FixedUpdate()
	{
		if (isServer)
		{
			transform.position = Vector3.MoveTowards(transform.position, _targetPosition, _currentSpeed * Time.fixedDeltaTime);
		}
	}
}