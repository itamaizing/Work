using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InputHandler : MonoBehaviour
{
	public static InputHandler Instance;

	private Vector2 _movementVector;
	private StandardInput _inputActions;

	public Vector2 MovementVector => _movementVector;

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
	public static UnityAction OnSixthAbility = delegate { };
	public static UnityAction OnSeventhAbility = delegate { };
	public static UnityAction OnEighthAbility = delegate { };

	public static UnityAction<int> OnFirstCast = delegate { };
	public static UnityAction<int> OnSecondCast = delegate { };
	public static UnityAction<int> OnThirdCast = delegate { };
	public static UnityAction<int> OnFourthCast = delegate { };
	public static UnityAction<int> OnFifthCast = delegate { };
	public static UnityAction<int> OnSixthCast = delegate { };
	public static UnityAction<int> OnSeventhCast = delegate { };
	public static UnityAction<int> OnEighthCast = delegate { };
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

		// spells 1-2-3-4-5-6-7-8
		_inputActions.GameplayMap.Spell1.performed += i => OnFirstAbility?.Invoke();
		_inputActions.GameplayMap.Spell2.performed += i => OnSecondAbility?.Invoke();
		_inputActions.GameplayMap.Spell3.performed += i => OnThirdAbility?.Invoke();
		_inputActions.GameplayMap.Spell4.performed += i => OnFourthAbility?.Invoke();
		_inputActions.GameplayMap.Spell5.performed += i=> OnFifthAbility?.Invoke();
		_inputActions.GameplayMap.Spell6.performed += i => OnSixthAbility?.Invoke();
		_inputActions.GameplayMap.Spell7.performed += i => OnSeventhAbility?.Invoke();
		_inputActions.GameplayMap.Spell8.performed += i => OnEighthAbility?.Invoke();

		_inputActions.GameplayMap.Spell1.performed += i => OnFirstCast?.Invoke(0);
		_inputActions.GameplayMap.Spell2.performed += i => OnSecondCast?.Invoke(1);
		_inputActions.GameplayMap.Spell3.performed += i => OnThirdCast?.Invoke(2);
		_inputActions.GameplayMap.Spell4.performed += i => OnFourthCast?.Invoke(3);
		_inputActions.GameplayMap.Spell5.performed += i => OnFifthCast?.Invoke(4);
		_inputActions.GameplayMap.Spell6.performed += i => OnSixthCast?.Invoke(5);
		_inputActions.GameplayMap.Spell7.performed += i => OnSeventhCast?.Invoke(6);
		_inputActions.GameplayMap.Spell8.performed += i => OnEighthCast?.Invoke(7);

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
