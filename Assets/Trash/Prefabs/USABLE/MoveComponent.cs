using DG.Tweening;
using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class MoveComponent : NetworkBehaviour
{
	[SerializeField, Range(0, 0.5f)] private float _smoothTime = 0.15f;
	[SerializeField] private float _currentSpeed = 5;
	[SerializeField] private Animator _anim;

	public Vector3 MoveDirection = Vector3.zero;
	
	public bool CanMove = false;
	public bool IsMoving = false;
	public bool IsSelect = false;
	
	private Rigidbody _rigidbody;

	private Vector3 _offset = Vector3.zero;

	private bool _isHero = false;

	private float _defaultSpeed = 5;
	private Camera _camera;

	private bool _isLookAtCursor = true;
	private Vector3 _dir;
	private Vector3 _currentVelocity;
	private Vector3 _currentVelocityTemp;
	private Coroutine _lookAtTransformJob;

    public void SetOffset(Vector2 offset)
	{
		_offset = offset;
	}

	public void Initialize(float speed, Rigidbody rb , bool isHero = false)
	{
		_defaultSpeed = speed;

		_rigidbody = rb;
		
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

	public void LookAtPosition(Vector3 position)
    {
		_isLookAtCursor = false;

		var transformRotate = transform.eulerAngles;
		transform.LookAt(position);
		transform.eulerAngles = (new Vector3(transformRotate.x, transform.eulerAngles.y, transformRotate.z));
	}

	public void LookAtTransform(Transform transform)
    {
		_lookAtTransformJob = StartCoroutine(lookAtTransformCoroutine(transform));
    }

	public void StopLookAt()
    {
		if(_lookAtTransformJob != null)
			StopCoroutine(_lookAtTransformJob);

		_isLookAtCursor = true;
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
		if (isOwned == false)
			return;

		if (_camera == null)
			_camera = Camera.main;

		Move();
		RotateAtCursor();
	}

	private void Move()
    {
		if (!CanMove || _rigidbody == null)
		{
			return;
		}

		if (IsSelect == false)
		{
			_dir = Vector2.zero;
		}

		if (_camera == null)
			return;

		_currentVelocity = Vector3.SmoothDamp(_currentVelocity, _dir, ref _currentVelocityTemp, _smoothTime); // Move from camera

		var camDir = _camera.transform.TransformDirection(_currentVelocity);

		camDir = Quaternion.AngleAxis(-_camera.transform.eulerAngles.x, _camera.transform.TransformVector(Vector3.right)) * camDir;

		_rigidbody.velocity = new Vector3(camDir.x * _currentSpeed, _rigidbody.velocity.y, camDir.z * _currentSpeed);

		var animDir = transform.InverseTransformPoint(transform.position + camDir);
		_anim.SetFloat(HashAnimPlayer.VelocityZ, animDir.z);
		_anim.SetFloat(HashAnimPlayer.VelocityX, animDir.x);
	}

	private void RotateAtCursor()
    {
		if (IsSelect == true && _isLookAtCursor == true)
		{
			Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;

			if (Physics.Raycast(ray, out hit))
			{
				var transformRotate = transform.eulerAngles;
				transform.LookAt(hit.point);
				transform.eulerAngles = (new Vector3(transformRotate.x, transform.eulerAngles.y, transformRotate.z));
			}
		}
	}

	private void OnMove(Vector2 dir)
    {
		if (IsSelect)
			_dir = new Vector3(dir.x, 0, dir.y);
	}

	private IEnumerator lookAtTransformCoroutine(Transform transform)
    {
		LookAtPosition(transform.position);
		yield return null;
    }

	[TargetRpc]
	public void TargetRpcAddForce(Vector3 vector3)
    {
		_rigidbody.AddForce(vector3);
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