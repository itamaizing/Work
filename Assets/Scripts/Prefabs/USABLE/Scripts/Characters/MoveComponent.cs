using Mirror;
using Pathfinding;
using Pathfinding.RVO;
using UnityEngine;
using UnityEngine.InputSystem;

public class MoveComponent : NetworkBehaviour
{
	private Vector2 _offset = Vector2.zero;
	private AIPath _agent;
	private RVOController _agentRvo;

	public bool CanMove;
	public bool IsMoving;
	public bool IsSelect;
	
	public Vector2 MoveDirection;

	private float _defaultSpeed;

	private bool _isHero = false;

	public void Initialize(float speed, AIPath agent, RVOController agentRvo , bool isHero = false)
	{
		_defaultSpeed = speed;
		_agent = agent;
		_agent.constrainInsideGraph = true;
		_agentRvo = agentRvo;
		
		_isHero = isHero;
		
		SetDefaultSpeed();

		CanMove = true;
	} 
	
	public void SetOffset(Vector2 offset)
	{
		_offset = offset;
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

	[ServerCallback]
	private void LateUpdate()
	{
		if (!_agent.pathPending && _agent.reachedEndOfPath)
		{
			_agentRvo.priority = _isHero ? 0.6f : 0.5f;
		}
		else if(_agent.pathPending)
		{
			_agentRvo.priority = _isHero ? 0.9f : 0.7f;
		}
		else if (_agent.reachedEndOfPath)
		{
			_agentRvo.priority = 0.3f;
		}
	}

	private void HandleMouseInput()
	{
		if(!Mouse.current.rightButton.wasPressedThisFrame) return;
		
		Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
		mousePosition.z = transform.position.z;
		CmdMove(mousePosition , _offset);
	}
	
	private void HandleKeyboardInput()
	{
		if(!_isHero) return;
		
		MoveDirection = new Vector2(
			Keyboard.current.aKey.isPressed ? -1 : Keyboard.current.dKey.isPressed ? 1 : 0,
			Keyboard.current.sKey.isPressed ? -1 : Keyboard.current.wKey.isPressed ? 1 : 0
		);

		if (MoveDirection != Vector2.zero)
		{
			CmdMove(transform.position + (Vector3)MoveDirection, Vector3.zero);
		}
	}

	[Command]
	private void CmdMove(Vector3 targetPosition , Vector3 offset)
	{
		NNInfo nearestNodeInfo = AstarPath.active.GetNearest(targetPosition);
		
		if (nearestNodeInfo.node is not { Walkable: true } || 
		    Vector3.Distance(targetPosition, nearestNodeInfo.position) > 0.6f)
		{
			return;
		}

		_agent.destination = targetPosition + offset;
	}
	
	[Command]
	public void UpdatePriority(float value)
	{
		_agentRvo.priority = value;
	}
	
}