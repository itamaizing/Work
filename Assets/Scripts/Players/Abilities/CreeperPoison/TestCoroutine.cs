using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class TestCoroutine : MonoBehaviour
{
    public bool ImBool = false;

    private void Update()
    {

    }

    IEnumerator enumerator(bool boo)
    {
        boo = true;
        yield return null;
    }
}
