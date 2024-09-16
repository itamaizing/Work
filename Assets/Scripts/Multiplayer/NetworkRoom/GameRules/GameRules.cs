using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GameRules : NetworkBehaviour
{
    protected readonly SyncList<GameObject> _players = new SyncList<GameObject>();
    protected List<Character> _playersSettings = new List<Character>();
    protected NetworkRoom _room;

    [SyncVar(hook = nameof(GameStatusHook))]
    private bool _isStarted;

    public bool IsStarted
    {
        get => _isStarted;
        set => _isStarted = value;
    }
    public SyncList<GameObject> Players => _players;

    public abstract void GameStartServer(List<Transform> spawnPoints);
    protected abstract void GameStartClient();

    public void Init(NetworkRoom room)
    {
        _room = room;

        foreach (var player in _room.Players)
        {
            _players.Add(player);
        }
    }

    protected void GameStatusHook(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            GameStartClient();
        }
    }

    protected IEnumerator SplitTeams(List<Transform> spawnPoints)
    {
        int team1Count = 0;
        int team2Count = 0;

        for (int i = 0; i < _players.Count; i++)
        {
            var player = _players[i];
            var playerSettings = player.GetComponent<UserNetworkSettings>();

            byte teamIndex = (byte)(team1Count <= team2Count ? 1 : 2);

            playerSettings.TeamIndex = teamIndex;

            Transform spawnPoint = teamIndex == 1 ? spawnPoints[0] : spawnPoints[1];
            if (spawnPoint != null)
            {
                player.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
                playerSettings.SetSpawnPosition(spawnPoint.position);
            }

            if (teamIndex == 1)
                team1Count++;
            else
                team2Count++;
        }

        yield return null;
    }

    protected IEnumerator SavePositionsAndAssignLayers()
    {
        foreach (var player in _players)
        {
            var playerSettings = player.GetComponent<UserNetworkSettings>();
            if (playerSettings != null)
            {
                playerSettings.SetSpawnPosition(player.transform.position);
                playerSettings.TargetUpdateLayers(playerSettings.connectionToClient);
            }
        }

        yield return null;
    }

    protected IEnumerator CloseRoomJob()
    {
        yield return null;
    }
}
