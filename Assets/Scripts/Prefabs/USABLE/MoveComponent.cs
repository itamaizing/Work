using Mirror;
using UnityEngine;

public class MoveComponent : NetworkBehaviour
{
	[SerializeField, Range(0, 0.5f)] private float _smoothTime = 0.15f;

	public Vector2 MoveDirection = Vector2.zero;
	
	public bool CanMove = false;
	public bool IsMoving = false;
	public bool IsSelect = false;
	
	private Rigidbody2D _rigidbody;
	private SpriteRenderer _spriteRenderer;

	private Vector2 _offset = Vector2.zero;

	private bool _isHero = false;

	private float _defaultSpeed = 5;
	private float _currentSpeed = 5;

	private Vector2 _dir;
	private Vector2 _currentVelocity;
	private Vector2 _currentVelocityTemp;

	public void SetOffset(Vector2 offset)
	{
		_offset = offset;
	}

	public void Initialize(float speed, Rigidbody2D rb, SpriteRenderer spriteRenderer, bool isHero = false)
	{
		_defaultSpeed = speed;

		_rigidbody = rb;
		_rigidbody.isKinematic = false;
		_spriteRenderer = spriteRenderer;

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
		if (!CanMove || _rigidbody == null)
		{
			return;
		}

		if (IsSelect == false)
			_dir = Vector2.zero;

		_currentVelocity = Vector2.SmoothDamp(_currentVelocity, _dir, ref _currentVelocityTemp, _smoothTime);
		_rigidbody.velocity = _currentVelocity * _currentSpeed;
	}

	void LateUpdate()
	{
		if (isLocalPlayer) CmdRotateAndScalePlayer(_dir);
	}


	private void OnMove(Vector2 dir)
    {
		if (IsSelect)
			_dir = dir;
	}

	[TargetRpc]
	public void TargetRpcAddForce(Vector2 vector2)
    {
		_rigidbody.AddForce(vector2);
	}

	[TargetRpc]
	public void TargetRpcAddTransformPosition(Vector3 vector3)
    {
		transform.position += vector3;
	}

	[TargetRpc]
	public void TargetRpcSetTransformPosition(Vector3 vector3)
    {
		transform.position = vector3;
	}


	[Command]
	private void CmdRotateAndScalePlayer(Vector2 direction)
	{
		RpcRotateAndScalePlayer(direction);
	}


	[ClientRpc]
	private void RpcRotateAndScalePlayer(Vector2 direction)
	{
		if (direction == Vector2.zero) return;
		float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
		Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
		_spriteRenderer.transform.rotation = Quaternion.Slerp(_spriteRenderer.transform.rotation, targetRotation, Time.deltaTime * 10f);
		_spriteRenderer.flipX = direction.x < 0;

		if (direction.x >= 0)
		{
			SetPlayerOrientation(new Vector3(-1f, 1f, 1f), false, false);
		}
		else
		{
			SetPlayerOrientation(new Vector3(1f, 1f, 1f), true, true);
		}
	}


	private void SetPlayerOrientation(Vector3 scale, bool flipX, bool flipY)
	{
		_spriteRenderer.transform.localScale = scale;
		_spriteRenderer.flipX = flipX;
		_spriteRenderer.flipY = flipY;
	}
}