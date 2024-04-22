using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMove : MonoBehaviour
{
	[Header("Movement Settings")]
	public float MoveSpeed = 5.0f;

	private Rigidbody2D _rigidbody;
	[HideInInspector] public bool CanMove = true;
	[HideInInspector] public bool IsMoving;
	[HideInInspector] public bool IsSelect;
	[HideInInspector] public Vector2 DirectionOfMovement;
	public SelectObject SelectObject;
	public GameObject CircleSelect;
	public GameObject MarkersSelect;
	public GameObject AbilityPanel;
	public List<Toggle> AbilitiesOnTargetToggles;

	private void Start()
	{
		_rigidbody = GetComponent<Rigidbody2D>();
		_rigidbody.isKinematic = true;
		SelectObject = GameObject.Find("Select").GetComponent<SelectObject>();
		DirectionOfMovement = Vector2.down;

		Deselect();
	}
	void Update()
	{
		if (SelectObject.SelectedObject == gameObject && IsSelect == false)
		{
			Select();
		}
		else if (SelectObject.SelectedObject != gameObject && IsSelect == true)
		{
			Deselect();
		}
	}

	void FixedUpdate()
	{
		if (!IsSelect || !CanMove)
		{
            _rigidbody.velocity = Vector2.zero;
            return;
		}

		if (InputHandler.Instance.MovementVector != Vector2.zero)
		{
			_rigidbody.isKinematic = false;
			_rigidbody.velocity = MoveSpeed * Time.fixedDeltaTime * InputHandler.Instance.MovementVector;
		}
		else
		{
			_rigidbody.velocity = Vector2.zero;
			_rigidbody.isKinematic = true;
		}

		IsMoving = _rigidbody.velocity != Vector2.zero;
	}

	private void Select()
	{
		IsSelect = true;
		CircleSelect.SetActive(true);
		AbilityPanel.SetActive(true);
		MarkersSelect.SetActive(true);
	}

	private void Deselect()
	{
		IsSelect = false;
		CircleSelect.SetActive(false);
		AbilityPanel.SetActive(false);
		MarkersSelect.SetActive(false);
	}
}



