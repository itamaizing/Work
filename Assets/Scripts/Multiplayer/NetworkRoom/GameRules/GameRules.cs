using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GameRules : NetworkBehaviour
{
    protected readonly SyncList<GameObject> _players = new SyncList<GameObject>();

    protected NetworkRoom _room;

    [SyncVar(hook = nameof(GameStatusHook))] private bool _isStarted;

    public bool IsStarted { get => _isStarted; set => _isStarted = value; }
    public SyncList<GameObject> Players => _players;

    protected abstract void GameStart();

    public void Init(NetworkRoom room)
    {
        _room = room;

        foreach (var item in _room.Players)
        {
            _players.Add(item);
        }
    }
    public void GameStatusHook(bool oldValue, bool newValue)
    {
        GameStart();
    }

    protected IEnumerator CloseRoomJob()
    {
        yield return StartCoroutine(_room.UnloadRoomJob());
    }
}
