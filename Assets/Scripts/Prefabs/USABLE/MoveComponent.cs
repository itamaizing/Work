using UnityEngine;

public class MoveComponent : MonoBehaviour
{
	private float _moveSpeed;
    private Rigidbody2D _rigidbody;
    private SelectComponent _selectComponent;

    [HideInInspector] public bool CanMove;
	[HideInInspector] public bool IsMoving;
	[HideInInspector] public bool IsSelect;
	[HideInInspector] public Vector2 MoveDirection;

	private float _defaultSpeed;

	private bool isInitialize = false;

    public void Initialize(float speed , Rigidbody2D rb)
	{
		_defaultSpeed = speed;
		_moveSpeed = speed;

		_rigidbody = rb;
		_rigidbody.isKinematic = true;

		MoveDirection = Vector2.down;

		_selectComponent = GetComponent<SelectComponent>();
		CanMove = true;
		isInitialize = true;
	}

	public void ChangeMoveSpeed(float value)
	{
		_moveSpeed *= value;
	}
	public void SetMoveSpeed(float speed)
	{
		_moveSpeed = speed;
	}
	public void SetDefaultSpeed()
	{
		_moveSpeed = _defaultSpeed;
	}
	void FixedUpdate()
	{
		if (!CanMove||!_selectComponent.IsSelect)
		{
            _rigidbody.velocity = Vector2.zero;
            return;
		}

		if (InputHandler.Instance.MovementVector != Vector2.zero)
		{
			_rigidbody.isKinematic = false;
			_rigidbody.velocity = _moveSpeed * Time.fixedDeltaTime * InputHandler.Instance.MovementVector;
		}
		else
		{
			_rigidbody.velocity = Vector2.zero;
			_rigidbody.isKinematic = true;
		}

		IsMoving = _rigidbody.velocity != Vector2.zero;
	}
}