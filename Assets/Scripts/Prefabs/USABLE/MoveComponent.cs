using Mirror;
using UnityEngine;

public class MoveComponent : NetworkBehaviour
{
	[SerializeField] private float _smoothTime = 0.15f;

	public Vector2 MoveDirection = Vector2.zero;
	
	public bool CanMove = false;
	public bool IsMoving = false;
	public bool IsSelect = false;
	
	private Rigidbody2D _rigidbody;

	private Vector2 _offset = Vector2.zero;

	private bool _isHero = false;

	private float _defaultSpeed;
	private float _currentSpeed;

	private Vector2 _dir;
	private Vector2 _currentVelocity;
	private Vector2 _currentVelocityTemp;

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

		InputHandler.OnPlayerMove += OnMove;
	}

    private void OnDestroy()
    {
		InputHandler.OnPlayerMove -= OnMove;
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
	
	[Client]
	void Update()
	{
		if (!CanMove || !IsSelect)
		{
			return;
		}
		_currentVelocity = Vector2.SmoothDamp(_currentVelocity, _dir, ref _currentVelocityTemp, _smoothTime);
	}

	private void OnMove(Vector2 dir)
    {
		_dir = dir;
	}

	[Client]
	private void FixedUpdate()
	{
		CmdMove(_currentVelocity * _currentSpeed);
	}

	[Command]
	private void CmdMove(Vector2 velocity)
	{
		_rigidbody.velocity = velocity;
	}
}