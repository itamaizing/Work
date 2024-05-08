using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CastLine : MonoBehaviour
{
    public Transform targetPoint;

    public void RotateAtMouse()
    {
        Vector3 dir = Input.mousePosition - Camera.main.WorldToScreenPoint(transform.position);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90);
    }

}
