using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

public abstract class GameRules : NetworkBehaviour
{
    [SerializeField] private int _maxPlayers = 2;
    [SerializeField] private int _expValuePerPlayer = 10;
    [SerializeField] private float _baseTimeForRevival = 5;
    [SerializeField] private float _AddTimeForRevival = 1;

    protected readonly SyncList<GameObject> _playersSyncList = new SyncList<GameObject>();
    protected List<Character> _players = new List<Character>();

    //protected NetworkRoom _room;

    [SyncVar] protected string _roomName;

    protected HeroSpawnManager _spawnPoints;
    protected PreparationAreaManager _preparationAreaManager;
    protected GameManager _gameManager;
    protected NpcSpawn _npcSpawn;
    protected Coroutine _regenCoroutine;

    [SyncVar] private bool _isStarted;
    private float _disconnectDelayClient = 6f;
    private float _disconnectDelayServer = 5f;
    private int _currentPlayers = 0;
    public bool IsStarted { get => _isStarted; set => _isStarted = value; }

    public SyncList<GameObject> Players => _playersSyncList;
    public HeroSpawnManager SpawnPoints => _spawnPoints;
    public int MaxPlayers { get => _maxPlayers;}
    public int CurrentPlayers { get => _currentPlayers; set => _currentPlayers = value; }

    public abstract void GameStartServer(HeroSpawnManager spawnPoints);
    protected abstract void UnsubscribeFromAllEvents();
    protected abstract void GameStartClient();
    protected abstract void OnPlayerDied(Character character);
    protected abstract void OnTowerDied(Object tower);
    protected abstract void RestartRound();

    protected void Restart()
    {
        RpcEnablePreparationAreas(5f);

        if (isServer)
        {
            if (_npcSpawn != null) _npcSpawn.DestroyAllNpc();
            List<NetworkIdentity> objectsToRemove = new List<NetworkIdentity>();

            foreach (var netIdentity in NetworkServer.spawned.Values)
            {
                if (netIdentity == null) continue;

                if (netIdentity.GetComponent<GameRules>() != null) continue;
                if (netIdentity.GetComponent<User>() != null) continue;
                if (netIdentity.GetComponent<MainTower>() != null) continue;
                if (netIdentity.GetComponent<HeroComponent>() != null) continue;

                objectsToRemove.Add(netIdentity);
            }

            foreach (var netIdentity in objectsToRemove) NetworkServer.Destroy(netIdentity.gameObject);

            if (_npcSpawn != null) _npcSpawn.SpawnAllNpc(gameObject.scene);
        }

        foreach (var playerSettings in _players)
        {
            if (playerSettings == null) continue;

            playerSettings.CharacterState.ServerClearAllStates();
            playerSettings.ServerResetAll();

            if (playerSettings.connectionToClient != null) playerSettings.NetworkSettings.TargetUpdateLayers(playerSettings.connectionToClient);

            if (playerSettings.Abilities != null)
            {
                playerSettings.Abilities.CancleAllSkills();

                foreach (var skill in playerSettings.Abilities.Skills)
                {
                    skill.RpcResetSkillState();
                }
            }

            int spawnIndex = playerSettings.NetworkSettings.TeamIndex - 1;

            if (_spawnPoints != null) RpcTeleportPlayer(playerSettings.gameObject, _spawnPoints.GetRandomPoint(spawnIndex), _spawnPoints.GetRotate(spawnIndex));
        }
    }

    public void CallRestartRound()
    {
        if (isServer) Restart();
        else CmdRestart();
    }

    public void Init()
    {
        //_room = room;
        //_roomName = _room.SceneName;
        Debug.LogError("asdasd");
        AddAllPlayersInList();
        SubscribingOnPlayerEvents();
        SubscribeToTowerDeath();

        StartCoroutine(FindServerGameManager());
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        StartCoroutine(FoundGameManagerCorounite());
    }

    public void CloseRoomOnClient()
    {
        ServerManager.Instance.EnableMenu();
        SceneManager.UnloadSceneAsync(_roomName);
        Destroy(gameObject);
    }

    //private void ResetResource(NetworkIdentity networkIdentity)
    //{
    //    if (networkIdentity.TryGetComponent<Resource>(out Resource resource)) resource.ResetValue();
    //}

    protected virtual void EndGame()
    {
        StartCoroutine(CloseRoomJob());
    }

    protected virtual IEnumerator FindServerGameManager()
    {
        while (SceneManager.GetActiveScene().isLoaded == false)
        {
            yield return null;
        }

        FindGameManager();
    }

    protected void FindGameManager()
    {
        _gameManager = FindAnyObjectByType<GameManager>();

        if (_gameManager == null) return;

        _spawnPoints = _gameManager.HeroSpawnManager;
        _preparationAreaManager = _gameManager.PreparationAreaManager;
        _npcSpawn = _gameManager.NpcSpawn;
        _gameManager.RestartRound.GameRules = this;

        if (_gameManager.TeamsPanel == null) return;
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

            UnityEngine.Debug.Log("Split teams", this);
            foreach (var player in _players)
            {
                playerSettings.NetworkSettings.Players.Add(player.gameObject);
            }
          /*  if (teamIndex == 1)
            {
                _gameManager.TeamsPanel.AddInFirstTeam(playerSettings);
                //RpcAddToTeam(teamIndex, playerSettings);
            }
            else
            {
                _gameManager.TeamsPanel.AddInSecondTeam(playerSettings);
                //RpcAddToTeam(teamIndex, playerSettings);
            }   */ 

            playerSettings.transform.SetPositionAndRotation(spawnPoints.GetRandomPoint(teamIndex-1), spawnPoints.GetRotate(teamIndex-1));
            playerSettings.NetworkSettings.SetSpawnPosition(spawnPoints.GetRandomPoint(teamIndex-1));

            if (teamIndex == 1)
            {
                team1Count++;
            }   
            else
            {
                team2Count++;
            }
                
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

        yield return new WaitForSecondsRealtime(_disconnectDelayClient);

        RpcCloseRoomOnClients();

        yield return new WaitForSecondsRealtime(_disconnectDelayServer);

        //yield return _room.UnloadRoomJob();

        if (User.Instance == null || User.Instance.isServer)
        {
            MPNetworkManager.Instance.StopServer();
        }
        else if (User.Instance.isClient)
        {
            MPNetworkManager.Instance.StopClient();
        }
    }

    protected virtual void AddExpForAllEnemy(Character character)
    {
        if (character is HeroComponent)
        {
            foreach (var player in _players)
            {
                if (character.NetworkSettings.TeamIndex != player.NetworkSettings.TeamIndex)
                {
                    player.LVL.AddEXP(_expValuePerPlayer);
                }
            }
        }
        else if (character is MinionComponent minion)
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
        float time = _baseTimeForRevival + _AddTimeForRevival * player.LVL.Value;
        //UnityEngine.Debug.Log("Try revive");
        RpcStartReviveTimer(player.gameObject, time);
        yield return new WaitForSecondsRealtime(time);
        player.ServerResetAll();
        MovePlayerInSpawn(player);
        _regenCoroutine = null;
    }

    
    private void AddAllPlayersInList()
    {
        foreach (var item in MPNetworkManager.Instance.Players)
        {
            _playersSyncList.Add(item);
            var playerSettings = item.GetComponent<Character>();
            if (playerSettings != null)
            {
                _players.Add(playerSettings);
            }
        }
    }

    private void SubscribeToTowerDeath()
    {
        var allTowers = GameObject.FindObjectsOfType<Object>().Where(obj => obj.IsTower == true);

        foreach (var tower in allTowers)
        {
            tower.Died += OnTowerDied;
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
        //UnityEngine.Debug.Log("FoundGameManagerCorounite");
        while (_gameManager == null || _gameManager.TeamsPanel == null || _gameManager.Source == null)
        {
            yield return new WaitForSecondsRealtime(0.5f);
            FindGameManager();

            if (_gameManager == null) continue;
            if (_gameManager.TeamsPanel == null) continue;
            if (_gameManager.Source == null) continue;
        }

        foreach (var item in _playersSyncList)
        {
            var playerSettings = item.GetComponent<Character>();
            if (playerSettings != null)
            {
                _players.Add(playerSettings);
            }

            if (playerSettings.NetworkSettings.TeamIndex == 1)
            {
                _gameManager.Source.AddInFirstTeam(playerSettings);
            }
            else
            {
                _gameManager.Source.AddInSecondTeam(playerSettings);
            }
        }

        //UnityEngine.Debug.Log("this");
        //foreach (var playerSettings in _players)
        //{
        //    UnityEngine.Debug.Log("123123123");
        //    if (playerSettings.NetworkSettings.TeamIndex == 1)
        //    {
        //        _gameManager.TeamsPanel.AddInFirstTeam(playerSettings);
        //        _gameManager.Source.AddInFirstTeam(playerSettings);
        //    }
        //    else
        //    {
        //        _gameManager.TeamsPanel.AddInSecondTeam(playerSettings);
        //        _gameManager.Source.AddInSecondTeam(playerSettings);
        //    }
        //}

        GameStartClient();
    }

    [ClientRpc]
    protected void RpcTeleportPlayer(GameObject player, Vector3 position, Quaternion rotation)
    {
        player.transform.SetPositionAndRotation(position, rotation);
    }

    [ClientRpc]
    protected void RpcStartReviveTimer(GameObject character, float time)
    {
        _gameManager.TeamsPanel.StartReviveTimer(character.GetComponent<Character>(), time);
    }

    [ClientRpc]
    protected virtual void RpcSetSource(int teamIndex, int source)
    {
        _gameManager.SourceUI.SetSource(teamIndex, source);
    }

    [ClientRpc]
    protected virtual void RpcShowWinner(int teamIndex)
    {
        _gameManager.SourceUI.ShowWinner(teamIndex);
    }

    [ClientRpc]
    protected void RpcCloseRoomOnClients()
    {
        CloseRoomOnClient();
    }

    [ClientRpc]
    protected void RpcAddToTeam(int index, Character hero)
    {
        if (index == 1)
        {
            _gameManager.TeamsPanel.AddInFirstTeam(hero);
        }
        else
        {
            _gameManager.TeamsPanel.AddInSecondTeam(hero);
        }
    }

    [ClientRpc] public void RpcEnablePreparationAreas(float duration) => _preparationAreaManager?.PreparationAreasDisable(duration);

    [Command(requiresAuthority = false)]
    public void CmdRestart()
    {
        Restart();
    }
}