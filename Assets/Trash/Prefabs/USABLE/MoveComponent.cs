using DG.Tweening;
using Mirror;
using UnityEngine;

public class MoveComponent : NetworkBehaviour
{
	[SerializeField, Range(0, 0.5f)] private float _smoothTime = 0.15f;

	public Vector2 MoveDirection = Vector2.zero;
	
	public bool CanMove = false;
	public bool IsMoving = false;
	public bool IsSelect = false;
	
	private Rigidbody _rigidbody;

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

	public void Initialize(float speed, Rigidbody rb , bool isHero = false)
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
		if (!CanMove || _rigidbody == null)
		{
			return;
		}

		if (IsSelect == false)
			_dir = Vector2.zero;

		_currentVelocity = Vector2.SmoothDamp(_currentVelocity, _dir, ref _currentVelocityTemp, _smoothTime);
		_rigidbody.velocity = _currentVelocity * _currentSpeed;
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
	[TargetRpc]
	public void TargetRpcDoMove(Vector3 vector3, float duration)
	{
		_rigidbody.DOMove(vector3, duration);
	}
	
}