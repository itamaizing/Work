using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GameRules : NetworkBehaviour
{
    protected readonly SyncList<GameObject> _players = new SyncList<GameObject>();
    protected List<UserNetworkSettings> _playersSettings = new List<UserNetworkSettings>();

    protected NetworkRoom _room;

    [SyncVar(hook = nameof(GameStatusHook))] private bool _isStarted;

    public bool IsStarted { get => _isStarted; set => _isStarted = value; }
    public SyncList<GameObject> Players => _players;

    public abstract void GameStartServer();
    protected abstract void GameStartClient();

    public void Init(NetworkRoom room)
    {
        _room = room;

        foreach (var item in _room.Players)
        {
            _players.Add(item);
        }
    }
    protected void GameStatusHook(bool oldValue, bool newValue)
    {
        GameStartClient();
    }

    protected virtual IEnumerator SplitIntoTeams()
    {
        for (int i = 0; i < _players.Count / 2; i++)
        {
            _playersSettings.Add(_players[i].GetComponent<UserNetworkSettings>());
            _playersSettings[i].TeamIndex = 1;
        }

        for (int i = _players.Count / 2; i < _players.Count; i++)
        {
            _playersSettings.Add(_players[i].GetComponent<UserNetworkSettings>());
            _playersSettings[i].TeamIndex = 2;
        }

        yield return new WaitForEndOfFrame();

        foreach (var item in _playersSettings)
        {
            foreach (var player in _playersSettings)
            {
                item.PlayersGameObject.Add(player.gameObject);
            }
            yield return new WaitForEndOfFrame();
            item.MarkUpEnemiesOrAllies();
        }
    }

    protected IEnumerator CloseRoomJob()
    {
        yield return StartCoroutine(_room.UnloadRoomJob());
    }
}
