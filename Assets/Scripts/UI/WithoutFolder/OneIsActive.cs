using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OneIsActive: MonoBehaviour
{
    public GameObject Obj1;
    public GameObject Obj2;

    public GameObject Icon1;
    public GameObject Icon2;


    public void OneActive()
    {
        if (Icon1.activeInHierarchy)
        {
            Obj1.GetComponent<MoveObjectsOnHideShow>().ToggleImageVisibility();
            Obj1.GetComponent<ButtonTalents>().SetButton();
        }
        if (Icon2.activeInHierarchy)
        {
            Obj2.GetComponent<MoveObjectsOnHideShow>().ToggleImageVisibility();
            Obj2.GetComponent<ButtonTalents>().SetButton();
        }
    }
}
