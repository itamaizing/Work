using Mirror;
using System;
using System.Collections;
using Pathfinding;
using UnityEngine;

//public class MoveComponent : NetworkBehaviour
//{
//	private Vector2 _offset = Vector2.zero;
	
//    private Rigidbody2D _rigidbody;

//    private Seeker _seeker;

//    private AIPath _agent;

//    private Vector2 target;

//    [HideInInspector] public bool CanMove;
//	[HideInInspector] public bool IsMoving;
//	[HideInInspector] public bool IsSelect;
//	[HideInInspector] public Vector2 MoveDirection;

//	private float _defaultSpeed;

//	private bool isInitialize = false;

//    public void Initialize(float speed , Rigidbody2D rb)
//	{
//		_defaultSpeed = speed;

//		_rigidbody = rb;
//		_rigidbody.isKinematic = false;

//		_seeker = GetComponent<Seeker>();
//		_agent = GetComponent<AIPath>();
//		SetDefaultSpeed();

//		MoveDirection = Vector2.down;
		
//		CanMove = true;
//		IsSelect = false;
//		isInitialize = true;
//	}
    
//	public void ChangeMoveSpeed(float value)
//	{
//		_agent.maxSpeed *= value;
//	}
//	public void SetMoveSpeed(float speed)
//	{
//		_agent.maxSpeed = speed;
//	}
//	public void SetDefaultSpeed()
//	{
//		_agent.maxSpeed = _defaultSpeed;
//	}

//	void FixedUpdate()
//	//private void SetMoveDirection()
//	//{
//	//	if(GetComponent<HeroComponent>()== null) return;
		
//	//	Vector2 move = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
		
//	//	if (move == Vector2.zero) return;
		
//	//	target= transform.position + (Vector3)move * _agent.maxSpeed;
		
//	//	_seeker.StartPath(transform.position,target);
		
//	//}

//	//public void SetOffset(Vector2 offset)
//	//{
//	//	_offset = offset;
//	//}

//	//void Update()
//	{
//		if(!isInitialize || !isLocalPlayer) return;
		
//		if (!IsSelect) { return;} 
		
//		SetMoveDirection();
		
//		if (Input.GetMouseButtonDown(1))
//		{
//			target = Camera.main.ScreenToWorldPoint(Input.mousePosition);

//		if (InputHandler.Instance.MovementVector != Vector2.zero)
//		{
//			_rigidbody.isKinematic = false;
//			var velocity = _moveSpeed* Time.fixedDeltaTime * InputHandler.Instance.MovementVector;
//			CmdMove(velocity);
//		}
//		else
//		{
//			CmdMove(Vector2.zero);
//			_rigidbody.isKinematic = true;
//		//	_seeker.StartPath(transform.position,target + _offset);
//		}

//		IsMoving = _agent.pathPending;
//	}

//    [Command]
//	private void CmdMove(Vector2 velocity)
//    {
//		_rigidbody.velocity = velocity;
//	}
//}

public class MoveComponent : NetworkBehaviour
{
	private Vector2 _offset = Vector2.zero; // new
	
	private Seeker _seeker;
	private AIPath _agent;
	
	private Rigidbody2D _rigidbody;
	
	private Vector2 target;

	public bool CanMove;
	public bool IsMoving;
	public bool IsSelect;
	public Vector2 MoveDirection;

	private float _defaultSpeed;

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
		
		_seeker = GetComponent<Seeker>(); 
		_agent = GetComponent<AIPath>();
		
		SetDefaultSpeed();

		MoveDirection = Vector2.down;

		CanMove = true;
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
		if (GetComponent<HeroComponent>() == null) return;

		Vector2 move = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

		if (move == Vector2.zero) return;

		target = transform.position + (Vector3)move * _agent.maxSpeed;
		
		CmdMove(target, Vector2.zero);
	}


	void FixedUpdate()
	{
		if (!isInitialize) return;

		if (!CanMove || !IsSelect)
		{
			return;
		}

		SetMoveDirection();

		if (Input.GetMouseButtonDown(1))
		{
			target = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			CmdMove(target,_offset);
		}
		
		IsMoving = _agent.pathPending;
	}

	[Command]
	private void CmdMove(Vector2 targetPos , Vector2 offset)
	{
		_seeker.StartPath(transform.position, targetPos + offset);
	}
}