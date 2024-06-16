using Pathfinding;
using UnityEngine;

public class MoveComponent : MonoBehaviour
{
	private Vector2 _offset = Vector2.zero;
	
    private Rigidbody2D _rigidbody;

    private Seeker _seeker;

    private AIPath _agent;

    private Vector2 target;

    [HideInInspector] public bool CanMove;
	[HideInInspector] public bool IsMoving;
	[HideInInspector] public bool IsSelect;
	[HideInInspector] public Vector2 MoveDirection;

	private float _defaultSpeed;

	private bool isInitialize = false;

    public void Initialize(float speed , Rigidbody2D rb)
	{
		_defaultSpeed = speed;

		_rigidbody = rb;

		_seeker = GetComponent<Seeker>();
		_agent = GetComponent<AIPath>();
		SetDefaultSpeed();

		MoveDirection = Vector2.down;
		
		CanMove = true;
		IsSelect = false;
		isInitialize = true;
	}
    
	public void ChangeMoveSpeed(float value)
	{
		_agent.maxSpeed *= value;
	}
	public void SetMoveSpeed(float speed)
	{
		_agent.maxSpeed = speed;
	}
	public void SetDefaultSpeed()
	{
		_agent.maxSpeed = _defaultSpeed;
	}

	private void SetMoveDirection()
	{
		if(GetComponent<HeroComponent>()== null) return;
		
		Vector3 move = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical") , 0);
		
		if (move == Vector3.zero) return;
		
		target= transform.position + move * _agent.maxSpeed;
		
		_seeker.StartPath(transform.position,target);
		
	}

	public void SetOffset(Vector2 offset)
	{
		_offset = offset;
	}

	void Update()
	{
		if(!isInitialize) return;
		
		if (!IsSelect) { return;} 
		
		SetMoveDirection();
		
		if (Input.GetMouseButtonDown(1))
		{
			target = Camera.main.ScreenToWorldPoint(Input.mousePosition);

			_seeker.StartPath(transform.position,target + _offset);
		}
		
		IsMoving = _agent.destination != transform.position;
	}
}