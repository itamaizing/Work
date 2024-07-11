using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestGameRules : GameRules
{
    [SerializeField] private float _lifeTime = 10f;

    protected override void GameStart()
    {
        if(isServer)
            StartCoroutine(CloseJob());
    }

    private IEnumerator CloseJob()
    {
        while(_lifeTime > 0)
        {
            _lifeTime -= Time.deltaTime;
            yield return null;
        }
        StartCoroutine(CloseRoomJob());
    }
}
