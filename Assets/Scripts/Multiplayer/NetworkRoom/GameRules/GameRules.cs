using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GameRules : NetworkBehaviour
{
    protected readonly SyncList<GameObject> _players = new SyncList<GameObject>();
    protected List<Character> _playersSettings = new List<Character>();
    protected NetworkRoom _room;
    protected List<Transform> _spawnPoints;

    [SyncVar(hook = nameof(GameStatusHook))] private bool _isStarted;
    public bool IsStarted { get => _isStarted; set => _isStarted = value; }

    public SyncList<GameObject> Players => _players;
    public List<Transform> SpawnPoints => _spawnPoints;

    public abstract void GameStartServer(List<Transform> spawnPoints);
    protected abstract void GameStartClient();

    public void Init(NetworkRoom room)
    {
        _room = room;

        foreach (var item in _room.Players)
        {
            _players.Add(item);
            var playerSettings = item.GetComponent<Character>();
            if (playerSettings != null)
            {
                _playersSettings.Add(playerSettings);
            }
        }

        StartCoroutine(WaitForSceneAndFindSpawnPoints());
    }

    protected virtual void GameStatusHook(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            GameStartClient();
        }
    }

    protected virtual IEnumerator WaitForSceneAndFindSpawnPoints()
    {
        while (!_room.IsLoaded)
        {
            yield return null;
        }

        FindSpawnPoints();
    }

    protected void FindSpawnPoints()
    {
        var spawnPointContainer = FindObjectOfType<SpawnPointsContainer>();
        if (spawnPointContainer != null)
        {
            _spawnPoints = spawnPointContainer.GetSpawnPoints();
        }
    }

    protected virtual IEnumerator SplitTeams(List<Transform> spawnPoints)
    {
        int team1Count = 0;
        int team2Count = 0;

        for (int i = 0; i < _playersSettings.Count; i++)
        {
            var playerSettings = _playersSettings[i];
            byte teamIndex = (byte)(team1Count <= team2Count ? 1 : 2);
            playerSettings.NetworkSettings.TeamIndex = teamIndex;

            Transform spawnPoint = spawnPoints[i % spawnPoints.Count];
            if (spawnPoint != null)
            {
                foreach (var player in _playersSettings)
                {
                    playerSettings.NetworkSettings.Players.Add(player.gameObject);
                }

                playerSettings.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
                playerSettings.NetworkSettings.SetSpawnPosition(spawnPoint.position);
            }

            if (teamIndex == 1)
                team1Count++;
            else
                team2Count++;
        }

        yield return null;
    }

    protected virtual IEnumerator SavePositionsAndAssignLayers()
    {
        foreach (var item in _playersSettings)
        {
            if (item != null)
            {
                item.NetworkSettings.SetSpawnPosition(item.transform.position);
                item.NetworkSettings.TargetUpdateLayers(item.connectionToClient);
            }
        }

        yield return null;
    }

    protected IEnumerator CloseRoomJob()
    {
        yield return StartCoroutine(_room.UnloadRoomJob());
    }
}
