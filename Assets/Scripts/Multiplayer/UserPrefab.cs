using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserPrefab : MonoBehaviour
{
    private void Start()
    {
        if(TryGetComponent<HeroComponent>(out HeroComponent hero))
        {
            //hero.IController = true;
        }
    }
}
