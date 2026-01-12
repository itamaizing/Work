using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestSpawnEnemy : MonoBehaviour
{
    public HeroComponent Hero;

    private void Awake()
    {
        Hero = GetComponent<HeroComponent>();
    }

    private void Update()
    {

        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            Hero.SpawnComponent.CmdSpawnCharacter(0, transform.position + Vector3.forward * 2f, Quaternion.identity, teamId: 2); //Враг
        }
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            Hero.SpawnComponent.CmdSpawnCharacter(0, transform.position + Vector3.forward * 2f, Quaternion.identity, teamId: 1); //Союзник
        }
    }
}
