using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InputHandler : MonoBehaviour
{
	public static InputHandler Instance;

	private Vector2 _movementVector;
	private StandardInput _inputActions;

	public Vector2 MovementVector { get { return _movementVector; } }

	#region Events

	public static UnityAction<Vector2> OnPlayerMove = delegate { };
	public static UnityAction OnClick = delegate { };
	public static UnityAction OnAltClick = delegate { };
	public static UnityAction OnDoubleAltClick = delegate { };

	public static UnityAction OnFirstAbility = delegate { };
	public static UnityAction OnSecondAbility = delegate { };
	public static UnityAction OnThirdAbility = delegate { };
	public static UnityAction OnFourthAbility = delegate { };
	public static UnityAction OnFifthAbility = delegate { };
	#endregion

	private void Awake()
	{
		if (Instance == null)
			Instance = this;
		else
			Destroy(Instance);

		_inputActions = new StandardInput();

		#region Events Listeners

		_inputActions.GameplayMap.Move.performed += i => OnPlayerMove?.Invoke(i.ReadValue<Vector2>());
		_inputActions.GameplayMap.Click.performed += i => OnClick?.Invoke();
		_inputActions.GameplayMap.AltClick.performed += i => OnAltClick?.Invoke();
		_inputActions.GameplayMap.AltDoubleClick.performed += i => OnDoubleAltClick?.Invoke();

		// spells 1-2-3-4-5
		_inputActions.GameplayMap.Spell1.performed += i => OnFirstAbility?.Invoke();
		_inputActions.GameplayMap.Spell2.performed += i => OnSecondAbility?.Invoke();
		_inputActions.GameplayMap.Spell3.performed += i => OnThirdAbility?.Invoke();
		_inputActions.GameplayMap.Spell4.performed += i => OnFourthAbility?.Invoke();
		_inputActions.GameplayMap.Spell5.performed += i=> OnFifthAbility?.Invoke();

		#endregion

		OnPlayerMove += OnMove;
	}

	private void OnEnable()
	{
		_inputActions.Enable();
	}

	private void OnDisable()
	{
		_inputActions.Disable();
	}

	private void OnMove(Vector2 value)
	{
		_movementVector = value;
	}
}
