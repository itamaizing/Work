using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GameRules : NetworkBehaviour
{
    [SyncVar(hook = nameof(GameStatusHook))] bool _isStarted;
    [SyncVar] private List<GameObject> _players;
    private NetworkRoom _room;

    public bool IsStarted { get => _isStarted; set => _isStarted = value; }
    public List<GameObject> Players { get => _players; set => _players = value; }

    protected abstract void GameStatusHook(bool oldValue, bool newValue);

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
