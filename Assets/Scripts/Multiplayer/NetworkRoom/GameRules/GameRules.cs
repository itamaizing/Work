using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class GameRules : NetworkBehaviour
{
    [SerializeField] private int _expValuePerPlayer = 10;
    [SerializeField] private float _baseTimeForRevival = 5;
    [SerializeField] private float _AddTimeForRevival = 1;

    protected readonly SyncList<GameObject> _playersSyncList = new SyncList<GameObject>();
    protected List<Character> _players = new List<Character>();
    protected NetworkRoom _room;

    protected HeroSpawnManager _spawnPoints;
    protected GameManager _gameManager;

    [SyncVar(hook = nameof(GameStatusHook))] private bool _isStarted;
    public bool IsStarted { get => _isStarted; set => _isStarted = value; }

    public SyncList<GameObject> Players => _playersSyncList;
    public HeroSpawnManager SpawnPoints => _spawnPoints;

    public abstract void GameStartServer(HeroSpawnManager spawnPoints);
    protected abstract void UnsubscribeFromAllEvents();
    protected abstract void GameStartClient();
    protected abstract void OnPlayerDied(Character character);

    public void Init(NetworkRoom room)
    {
        _room = room;

        AddAllPlayersInList();
        SubscribingOnPlayerEvents();

        StartCoroutine(WaitForSceneAndFindSpawnPoints());
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        StartCoroutine(FoundGameManagerCorounite());
    }

    //perhaps this method only works on the first client, it's strange
    protected virtual void GameStatusHook(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            GameStartClient();
            //perhaps this method only works on the first client, it's strange
        }
    }

    protected virtual IEnumerator WaitForSceneAndFindSpawnPoints()
    {
        while (!_room.IsLoaded)
        {
            yield return null;
        }

        FindGameManager();
    }

    protected void FindGameManager()
    {
        var gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            _gameManager = gameManager;
            _spawnPoints = _gameManager.HeroSpawnManager;
        }
    }

    protected virtual IEnumerator SplitTeams(HeroSpawnManager spawnPoints)
    {
        int team1Count = 0;
        int team2Count = 0;

        for (int i = 0; i < _players.Count; i++)
        {
            var playerSettings = _players[i];
            byte teamIndex = (byte)(team1Count <= team2Count ? 1 : 2);
            playerSettings.NetworkSettings.TeamIndex = teamIndex;

            foreach (var player in _players)
            {
                playerSettings.NetworkSettings.Players.Add(player.gameObject);
            }

            playerSettings.transform.SetPositionAndRotation(spawnPoints.GetRandomPoint(teamIndex-1), spawnPoints.GetRotate(teamIndex-1));
            playerSettings.NetworkSettings.SetSpawnPosition(spawnPoints.GetRandomPoint(teamIndex-1));

            if (teamIndex == 1)
                team1Count++;
            else
                team2Count++;
        }

        yield return null;
    }

    protected virtual IEnumerator SavePositionsAndAssignLayers()
    {
        foreach (var item in _players)
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
        UnsubscribeFromAllEvents();
        UnsubscribingOnPlayerEvents();

        yield return new WaitForSeconds(1f);

        if (_room != null)
        {
            yield return _room.UnloadRoomJob();
        }
    }

    protected virtual void AddExpForAllEnemy(Character character)
    {
        if(character is HeroComponent)
        {
            foreach (var player in _players)
            {
                if (character.NetworkSettings.TeamIndex != player.NetworkSettings.TeamIndex)
                {
                    player.LVL.AddEXP(_expValuePerPlayer);
                }
            }
        }
        else if(character is MinionComponent minion)
        {
            foreach (var player in _players)
            {
                if (character.NetworkSettings.TeamIndex != player.NetworkSettings.TeamIndex)
                {
                    player.LVL.AddEXP(minion.ExpForDieKill);
                }
            }
        }
    }

    protected virtual void SetSource(int teamIndex, int source)
    {
        _gameManager.SourceUI.SetSource(teamIndex, source);
        RpcSetSource(teamIndex, source);
    }

    [ClientRpc]
    protected virtual void RpcSetSource(int teamIndex, int source)
    {
        _gameManager.SourceUI.SetSource(teamIndex, source);
    }

    protected virtual void ResetAllPlayers()
    {
        foreach (var player in _players)
        {
            player.ServerResetAll();
        }
    }

    protected virtual void MoveAllPlayersInSpawnPoint()
    {
        foreach (var player in _players)
        {
            MovePlayerInSpawn(player);
        }
    }

    protected void MovePlayerInSpawn(Character player)
    {
        RpcTeleportPlayer(player.gameObject, _spawnPoints.GetRandomPoint(player.NetworkSettings.TeamIndex - 1), _spawnPoints.GetRotate(player.NetworkSettings.TeamIndex - 1));
    }

    protected IEnumerator RevivalPlayerCoroutine(Character player)
    {
        yield return new WaitForSecondsRealtime(_baseTimeForRevival + _AddTimeForRevival * player.LVL.Value);
        player.ServerResetAll();
        MovePlayerInSpawn(player);
    }

    private void AddAllPlayersInList()
    {
        foreach (var item in _room.Players)
        {
            _playersSyncList.Add(item);
            var playerSettings = item.GetComponent<Character>();
            if (playerSettings != null)
            {
                _players.Add(playerSettings);
            }
        }
    }

    private void SubscribingOnPlayerEvents()
    {
        foreach (var item in _players)
        {
            item.Died += OnPlayerDied;
        }
    }
    
    private void UnsubscribingOnPlayerEvents()
    {
        foreach (var item in _players)
        {
            item.Died -= OnPlayerDied;
        }
    }

    private IEnumerator FoundGameManagerCorounite()
    {
        while(_gameManager == null)
        {
            FindGameManager();
            yield return new WaitForSecondsRealtime(0.5f);
        }
    }

    [ClientRpc]
    protected void RpcTeleportPlayer(GameObject player, Vector3 position, Quaternion rotation)
    {
        player.transform.SetPositionAndRotation(position, rotation);
    }
}