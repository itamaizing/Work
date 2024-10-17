using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestController : MonoBehaviour
{
    [SerializeField, Range(0, 0.5f)] private float _smoothTime = 0.15f;
	[SerializeField] private Rigidbody _rigidbody;
	[SerializeField] private Animator _anim;
	[SerializeField] private float _currentSpeed = 5;
	[SerializeField] private float tempTest = 90;

	public Vector3 MoveDirection = Vector3.zero;

	public bool CanMove = false;
	public bool IsMoving = false;
	public bool IsSelect = true;

	private Vector3 _offset = Vector3.zero;

	private bool _isHero = false;

	private float _defaultSpeed = 5;

	private Vector3 _dir;
	private Vector3 _currentVelocity;
	private Vector3 _currentVelocityTemp;

    public Vector3 CurrentVelocity { get => _currentVelocity; set => _currentVelocity = value; }

    private void Awake()
    {
		Initialize();
    }

    public void SetOffset(Vector2 offset)
	{
		_offset = offset;
	}

	public void Initialize()
	{
		_rigidbody.isKinematic = false;

		SetDefaultSpeed();

		MoveDirection = Vector2.zero;

		CanMove = true;

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

	void Update()
	{
		if (!CanMove || _rigidbody == null)
		{
			return;
		}
		_currentVelocity = Vector3.SmoothDamp(_currentVelocity, _dir, ref _currentVelocityTemp, _smoothTime); // Move from camera
		//_rigidbody.velocity = _currentVelocity * _currentSpeed;

		var camDir = Camera.main.transform.TransformDirection(_currentVelocity);
		camDir = Quaternion.AngleAxis(-Camera.main.transform.eulerAngles.x, Camera.main.transform.TransformVector(Vector3.right)) * camDir;
		_rigidbody.velocity = camDir * _currentSpeed;


		/*
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;
		if(Physics.Raycast(ray, out hit))
        {
			var transformRotate = transform.eulerAngles;
			transform.LookAt(hit.point);
			transform.eulerAngles = (new Vector3(transformRotate.x, transform.eulerAngles.y, transformRotate.z));
		}*/

		var mousePos = Input.mousePosition; // dir from curcore - player for anim
		var playerPos = Camera.main.WorldToScreenPoint(transform.position);

		var dir = mousePos - playerPos;

		var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

		transform.rotation = Quaternion.AngleAxis(-angle + 90, Vector3.up);
		var animDir = transform.InverseTransformPoint(transform.position + _currentVelocity);
		_anim.SetFloat("Y", animDir.z);
		_anim.SetFloat("X", animDir.x);

		transform.rotation = Quaternion.AngleAxis(-angle + Camera.main.transform.eulerAngles.y + 90, Vector3.up);
	}

	private void OnMove(Vector2 dir)
	{
		_dir = new Vector3 (dir.x, 0, dir.y);
	}

	public void TargetRpcAddForce(Vector2 vector2)
	{
		_rigidbody.AddForce(vector2);
	}

	public void TargetRpcAddTransformPosition(Vector3 vector3)
	{
		transform.position += vector3;
	}

	public void TargetRpcSetTransformPosition(Vector3 vector3)
	{
		transform.position = vector3;
	}
}
