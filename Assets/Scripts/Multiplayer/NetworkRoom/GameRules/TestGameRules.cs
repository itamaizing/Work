using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestGameRules : GameRules
{
    [SerializeField] private float _lifeTime = 10f;
    [SerializeField] private bool isRemoveRoom = true;

    public override void GameStartServer()
    {
        StartCoroutine(SplitIntoTeams());

        if (isServer && isRemoveRoom)
            StartCoroutine(CloseJob());
    }

    protected override void GameStartClient()
    {
        
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
