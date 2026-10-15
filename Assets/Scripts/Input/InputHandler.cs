using System;
using System.Collections;
using System.Collections.Generic;
using kcp2k;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
	public static InputHandler Instance;

	private string[] _keySpell = new string[16];

	private Vector2 _movementVector;
	private StandardInput _inputActions;
	private bool _IsClick;

	public Vector2 MovementVector => _movementVector;

	public StandardInput InputActions { get => _inputActions; set => _inputActions = value; }

	#region Events

	public static bool Click { get => Instance._IsClick; }

	public static UnityAction<Vector2> OnPlayerMove = delegate { };
	public static UnityAction OnClick = delegate { };
	public static UnityAction OnClickCanceled = delegate { };
	public static UnityAction OnAltClick = delegate { };
	public static UnityAction OnDoubleAltClick = delegate { };
	public static UnityAction OnSwitchAutoMode = delegate { };
	public static UnityAction OnSwitchAutoModeCanceled = delegate { };
	public static UnityAction OnShiftLeftMouse = delegate { };
	public static UnityAction OnShiftLeftMouseCanceled = delegate { };
	public static UnityAction OnSpacetLeftMouse = delegate { };
	public static UnityAction OnSpacetLeftMouseCanceled = delegate { };
	public static UnityAction<float> ScrollMouse = delegate { };
	public static UnityAction ShowMenu = delegate { };
	public static UnityAction ShowStatistics = delegate { };
	public static UnityAction ShowSource = delegate { };

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

	public static UnityAction<int> OnCast = delegate { };
	#endregion

	private static bool IsBlocked(InputLayer layer) => InputBlocker.IsBlocked(layer);

	private static void Bind(InputAction action, InputLayer layer, Action onPerformed, Action onCanceled = null, bool pointer = false)
	{
		if (onPerformed != null)
			action.performed += _ =>
			{
				if (pointer) InputBlocker.NotifyPointerPress();
				if (!IsBlocked(layer)) onPerformed();
			};

		if (onCanceled != null)
			action.canceled += _ => onCanceled();
	}

	private static void BindValue<T>(InputAction action, InputLayer layer, Action<T> onPerformed) where T : struct
	{
		action.performed += c => { if (!IsBlocked(layer)) onPerformed(c.ReadValue<T>()); };
	}

	private static bool PointerHitsUI()
	{
		if (EventSystem.current == null)
			return false;

		PointerEventData eventData = new(EventSystem.current)
		{
			position = Input.mousePosition
		};

		List<RaycastResult> results = new();
		EventSystem.current.RaycastAll(eventData, results);
		return results.Count > 0;
	}

	private void Awake()
	{
		if (Instance == null)
		{
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
		else
		{
            Destroy(Instance);
        }
		_inputActions = new StandardInput();

		var map = _inputActions.GameplayMap;

		#region Events Listeners

		const InputLayer G = InputLayer.Gameplay;

		BindValue<Vector2>(map.Move, G, v => OnPlayerMove?.Invoke(v));

		Bind(map.Click, G,
			() =>
			{
				if (PointerHitsUI())
					return;
				OnClick?.Invoke();
			},
			() => OnClickCanceled?.Invoke(),
			pointer: true);

		Bind(map.AltClick, G, () => OnAltClick?.Invoke(), pointer: true);
		Bind(map.AltDoubleClick, G, () => OnDoubleAltClick?.Invoke(), pointer: true);
		Bind(map.SwitchAutoMode, G, () => OnSwitchAutoMode?.Invoke(), () => OnSwitchAutoModeCanceled?.Invoke());
		Bind(map.ShiftMouse, G, () => OnShiftLeftMouse?.Invoke(), () => OnShiftLeftMouseCanceled?.Invoke(), pointer: true);
		Bind(map.SpaceMouse, G, () => OnSpacetLeftMouse?.Invoke(), () => OnSpacetLeftMouseCanceled?.Invoke(), pointer: true);

		BindValue<float>(map.ScrollMouse, InputLayer.Camera, v => ScrollMouse?.Invoke(v));

		Bind(map.ShowMenu, InputLayer.Hotkeys, () => ShowMenu?.Invoke());
		Bind(map.ShowStatistics, InputLayer.Hotkeys, () => ShowStatistics?.Invoke());
		Bind(map.ShowSource, InputLayer.Hotkeys, () => ShowSource?.Invoke());

		// spells 1-2-3-4-5-6-7-8 (по порядку, как раньше: Ability -> конкретный Cast -> общий OnCast)
		Bind(map.Spell1, G, () => { OnFirstAbility?.Invoke();   OnFirstCast?.Invoke(0);   OnCast?.Invoke(0); });
		Bind(map.Spell2, G, () => { OnSecondAbility?.Invoke();  OnSecondCast?.Invoke(1);  OnCast?.Invoke(1); });
		Bind(map.Spell3, G, () => { OnThirdAbility?.Invoke();   OnThirdCast?.Invoke(2);   OnCast?.Invoke(2); });
		Bind(map.Spell4, G, () => { OnFourthAbility?.Invoke();  OnFourthCast?.Invoke(3);  OnCast?.Invoke(3); });
		Bind(map.Spell5, G, () => { OnFifthAbility?.Invoke();   OnFifthCast?.Invoke(4);   OnCast?.Invoke(4); });
		Bind(map.Spell6, G, () => { OnSixthAbility?.Invoke();   OnSixthCast?.Invoke(5);   OnCast?.Invoke(5); });
		Bind(map.Spell7, G, () => { OnSeventhAbility?.Invoke(); OnSeventhCast?.Invoke(6); OnCast?.Invoke(6); });
		Bind(map.Spell8, G, () => { OnEighthAbility?.Invoke();  OnEighthCast?.Invoke(7);  OnCast?.Invoke(7); });

		Bind(map.Spell9,  G, () => OnCast?.Invoke(8));
		Bind(map.Spell10, G, () => OnCast?.Invoke(9));
		Bind(map.Spell11, G, () => OnCast?.Invoke(10));
		Bind(map.Spell12, G, () => OnCast?.Invoke(11));
		Bind(map.Spell13, G, () => OnCast?.Invoke(12));
		Bind(map.Spell14, G, () => OnCast?.Invoke(13));
		Bind(map.Spell15, G, () => OnCast?.Invoke(14));
		Bind(map.Spell16, G, () => OnCast?.Invoke(15));

		#endregion

		OnClick += SetClickTrue;
		OnClickCanceled += SetClickFalse;
	}

    private void Start()
    {
		MPNetworkManager.Instance.ConnectClosed += OnClientDisconnect;
    }

    private void OnClientDisconnect()
    {
		Instance = null;
        Destroy(gameObject);
    }

    private void OnEnable()
	{
		if(_inputActions != null)
		_inputActions.Enable();
	}

	private void OnDisable()
	{
        if (_inputActions != null)
            _inputActions.Disable();
	}

	private void SetClickTrue()
	{
		_IsClick = true;
	}

	private void SetClickFalse()
	{
		_IsClick = false;
	}

	[ContextMenu("HELPME")]
	private void help()
	{
		string ipAndPort = "89.169.1.90:7778";

        string ip;
        string port;

        string[] parts = ipAndPort.Split(':');

        if (parts.Length == 2)
        {
            ip = parts[0];
            port = parts[1];

            Debug.Log($"Try connect - {ip}:{port}");

            var transport = MPNetworkManager.Instance.GetComponent<KcpTransport>();

            if (transport == null)
            {
                Debug.LogError("Transport not found on NetworkManager");
                return;
            }
            transport.port = ushort.Parse(port);
            MPNetworkManager.Instance.networkAddress = ip;

            MPNetworkManager.Instance.StartClient();
        }
        else
        {
            Debug.LogError("Samthing wrong " + parts.Length);
            Debug.LogError(parts);
        }
    }
}