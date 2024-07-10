using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GameRules : NetworkBehaviour
{
    [SyncVar] protected List<GameObject> _players;
    protected NetworkRoom _room;
    [SyncVar(hook = nameof(GameStatusHook))] private bool _isStarted;

    public bool IsStarted { get => _isStarted; set => _isStarted = value; }
    public List<GameObject> Players { get => _players; set => _players = value; }

    public abstract void GameStatusHook(bool oldValue, bool newValue);

    public void Init(NetworkRoom room)
    {
        _room = room;
        _players = _room.Players;
    }

    protected IEnumerator CloseRoomJob()
    {
        yield return StartCoroutine(_room.UnloadRoomJob());
    }
}
