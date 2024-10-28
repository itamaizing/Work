using DG.Tweening;
using Mirror;
using UnityEngine;

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

	private Vector3 _dir;
	private Vector3 _currentVelocity;
	private Vector3 _currentVelocityTemp;

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
		if (!CanMove || _rigidbody == null || _camera == null)
		{
			_camera = Camera.main;
			return;
		}
		
		if (IsSelect == false)
        {
			_dir = Vector2.zero;
		}
			

		_currentVelocity = Vector3.SmoothDamp(_currentVelocity, _dir, ref _currentVelocityTemp, _smoothTime); // Move from camera

		var camDir = _camera.transform.TransformDirection(_currentVelocity);

		camDir = Quaternion.AngleAxis(-_camera.transform.eulerAngles.x, _camera.transform.TransformVector(Vector3.right)) * camDir;

		_rigidbody.velocity = camDir * _currentSpeed;

		if(IsSelect == true)
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

		var animDir = transform.InverseTransformPoint(transform.position + camDir);
		_anim.SetFloat("Y", animDir.z);
		_anim.SetFloat("X", animDir.x);
		
	}

	private void OnMove(Vector2 dir)
    {
		if (IsSelect)
			_dir = new Vector3(dir.x, 0, dir.y);
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