using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;

public class SelectObject : MonoBehaviour
{
    [HideInInspector] public GameObject SelectedObject;
    [HideInInspector] public bool CanSelect = true;
    public GameObject FirstPlayer;
    public GameObject SecondPlayer;
    public GameObject ThirstPlayer;
    public List<GameObject> ControlledObjects = new List<GameObject>();

    private void Start()
    {
        if(SelectedObject != null)
        {
            SelectedObject.layer = LayerMask.NameToLayer("Player");
        }
    }
    private void Update()
    {
        if (CanSelect)
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                if (SelectedObject != null)
                {
                    SelectedObject.layer = LayerMask.NameToLayer("OtherPlayers");
                }

                SelectedObject = FirstPlayer;
                SelectedObject.layer = LayerMask.NameToLayer("Player");
            }
            if (Input.GetKeyDown(KeyCode.F2))
            {
                if (SelectedObject != null)
                {
                    SelectedObject.layer = LayerMask.NameToLayer("OtherPlayers");
                }

                SelectedObject = SecondPlayer;
                SelectedObject.layer = LayerMask.NameToLayer("Player");

            }
            if (Input.GetKeyDown(KeyCode.F3))
            {
                if (SelectedObject != null)
                {
                    SelectedObject.layer = LayerMask.NameToLayer("OtherPlayers");
                }

                SelectedObject = ThirstPlayer;
                SelectedObject.layer = LayerMask.NameToLayer("Player");

            }
            if (ControlledObjects != null && Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    if (SelectedObject != null)
                    {
                        SelectedObject.layer = LayerMask.NameToLayer("OtherPlayers");
                    }

                    if (ControlledObjects.Count > 0)
                    {
                        SelectedObject = ControlledObjects[0];
                        SelectedObject.layer = LayerMask.NameToLayer("Player");
                    }
                }
            }
            if (ControlledObjects != null && Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    if (SelectedObject != null)
                    {
                        SelectedObject.layer = LayerMask.NameToLayer("OtherPlayers");
                    }

                    if (ControlledObjects.Count > 1)
                    {
                        SelectedObject = ControlledObjects[1];
                        SelectedObject.layer = LayerMask.NameToLayer("Player");
                    }
                }
            }
        }

        if(ControlledObjects.Count > 0)
        {
            if (ControlledObjects[0] == null &&  ControlledObjects.Count > 1)
            {
                ControlledObjects.Remove(ControlledObjects[0]);
            }
        }
    }
}
