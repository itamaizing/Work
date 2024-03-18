using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMove : MonoBehaviour
{
    private Rigidbody2D _rigidbody;
    [HideInInspector] public bool  CanMove = true;
    [HideInInspector] public bool IsMoving;
    [HideInInspector] public bool IsSelect;
    [HideInInspector] public Vector2 DirectionOfMovement;
    public float MoveSpeed = 5.0f;
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
        else if(SelectObject.SelectedObject != gameObject && IsSelect == true)
        {
            Deselect();
        }

        if (Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.S) || Input.GetKeyUp(KeyCode.A) || Input.GetKeyUp(KeyCode.D))
        {
            _rigidbody.velocity = Vector2.zero;
            _rigidbody.isKinematic = true;
        }

        IsMoving = _rigidbody.velocity != Vector2.zero;

        if (IsMoving)
        {
            //DirectionOfMovement = _rigidbody.velocity.normalized;
        }
    }

    void FixedUpdate()
    {
        if (IsSelect && CanMove)
        {
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
            {
                _rigidbody.isKinematic = false;
                _rigidbody.velocity = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")) * 100 * MoveSpeed * Time.deltaTime;
            }
        }
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
  


