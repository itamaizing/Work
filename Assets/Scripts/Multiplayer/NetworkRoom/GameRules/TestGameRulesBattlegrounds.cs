using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TestGameRulesBattlegrounds : GameRules
{
    private List<GameObject> _towerTeam1;
    private List<GameObject> _towerTeam2;

    [Header("Game Settings")]
    [SerializeField] private float _lifeTime = 10f;
    [SerializeField] private int _experienceForKill = 5;
    [SerializeField] private int _experienceForWin = 3;
    [SerializeField] private float _bottleVolumeForWin = 0.33f;

    [SerializeField] private bool isRemoveRoom = true;

    public override void GameStartServer(List<Transform> spawnPoints)
    {
        StartCoroutine(HandleTeamsAndSpawns(spawnPoints));
        FindTeamTowers();

        foreach (var playerSettings in _players)
        {
            //if (playerSettings is HeroComponent heroComponent)
            //{
            //    heroComponent.TalentManager.ClientRpcResetTalentPoints();
            //    SaveManager.Instance.ResetAllTalents(heroComponent);
            //}

            var health = playerSettings.NetworkSettings.CachedHealth;
            if (health != null)
            {
                health.Died += () => OnPlayerDeath(playerSettings);
                health.Died += () => RespawnPlayer(playerSettings);
                health.Died += () => ResetPlayerState(playerSettings);

                playerSettings.LVL.LVLUped += (newLevel) => OnPlayerLevelUp(playerSettings);
            }
        }

        SubscribeToTowerDeaths();
    }

    private void OnPlayerLevelUp(Character playerSettings)
    {
        int playerTeamIndex = playerSettings.NetworkSettings.TeamIndex;

        //foreach (var player in _players)
        //{
        //    if (player.NetworkSettings.TeamIndex == playerTeamIndex && player is HeroComponent heroComponent)
        //    {
        //        heroComponent.TalentManager.ClientRpcAddPoints();
        //        Debug.Log($"Player in Team {playerTeamIndex} received 1 talent point due to level up.");
        //    }
        //}
    }

    protected override void GameStartClient()
    {
    }

    private void FindTeamTowers()
    {
        TowerTeam towerTeam = FindObjectOfType<TowerTeam>();
        if (towerTeam != null)
        {
            _towerTeam1 = new List<GameObject>(towerTeam.TowerTeam_1);
            _towerTeam2 = new List<GameObject>(towerTeam.TowerTeam_2);
        }

        if (_towerTeam1.Count == 0 || _towerTeam2.Count == 0)
        {
            Debug.LogError("No towers were found for one or both teams!");
        }
    }

    private void SubscribeToTowerDeaths()
    {
        foreach (var tower in _towerTeam1)
        {
            var objectHealth = tower.GetComponent<ObjectHealth>();
            if (objectHealth != null)
            {
                objectHealth.OnDeath += () => OnTowerDestroyed(1);
            }
        }

        foreach (var tower in _towerTeam2)
        {
            var objectHealth = tower.GetComponent<ObjectHealth>();
            if (objectHealth != null)
            {
                objectHealth.OnDeath += () => OnTowerDestroyed(2);
            }
        }
    }

    private void OnPlayerDeath(Character playerSettings)
    {
        Debug.Log($"Player from Team {playerSettings.NetworkSettings.TeamIndex} died.");

        int enemyTeamIndex = playerSettings.NetworkSettings.TeamIndex == 1 ? 2 : 1;

        foreach (var player in _players)
        {
            if (player.NetworkSettings.TeamIndex == enemyTeamIndex)
            {
                player.LVL.AddEXP(_experienceForKill);
            }
        }
    }

    private void ResetPlayerState(Character playerSettings)
    {
        var health = playerSettings.Health;
        health?.ResetValue();

        var runeComponent = playerSettings.GetComponent<RuneComponent>();
        runeComponent?.ResetValueRune();

        var characterState = playerSettings.CharacterState;
        if (characterState != null)
        {
            var statesCopy = new List<AbstractCharacterState>(characterState.CurrentStates);
            foreach (var state in statesCopy)
            {
                characterState.RemoveState(state.State);
            }
        }
    }

    private void RespawnPlayer(Character playerSettings)
    {
        int spawnIndex = playerSettings.NetworkSettings.TeamIndex - 1;
        if (_spawnPoints != null && spawnIndex >= 0 && spawnIndex < _spawnPoints.Count)
        {
            Transform spawnPoint = _spawnPoints[spawnIndex];
            RpcTeleportPlayer(playerSettings.gameObject, spawnPoint.position, spawnPoint.rotation);
        }
    }

    private void OnTowerDestroyed(int teamIndex)
    {
        EndGame(teamIndex);
    }

    private void EndGame(int losingTeamIndex)
    {
        bool isTeam1Winner = losingTeamIndex == 2;
        bool isTeam2Winner = losingTeamIndex == 1;

        var user = User.Instance ?? FindObjectOfType<User>();

        var bottleManager = BottleUserManager.Instance;
        var levelManager = LevelCharacterManager.Instance;

        if (isTeam1Winner || isTeam2Winner)
        {
            levelManager.AddExperience(_experienceForWin);
            bottleManager.AddBottleVolume(_bottleVolumeForWin);
        }

        RpcCloseRoomOnClients();
        StartCoroutine(CloseRoomJob());
    }

    protected override void UnsubscribeFromAllEvents()
    {
        foreach (var playerSettings in _players)
        {
            var health = playerSettings.NetworkSettings.CachedHealth;
            if (health != null)
            {
                health.Died -= () => OnPlayerDeath(playerSettings);
                health.Died -= () => RespawnPlayer(playerSettings);
                health.Died -= () => ResetPlayerState(playerSettings);
            }

            var runeComponent = playerSettings.GetComponent<RuneComponent>();
            if (runeComponent != null && health != null)
            {
                health.Died -= runeComponent.ResetValue;
            }

            playerSettings.LVL.LVLUped -= (newLevel) => OnPlayerLevelUp(playerSettings);
        }

        if (_towerTeam1 != null)
        {
            foreach (var tower in _towerTeam1)
            {
                var objectHealth = tower.GetComponent<ObjectHealth>();
                if (objectHealth != null)
                {
                    objectHealth.OnDeath -= () => OnTowerDestroyed(1);
                }
            }
        }

        if (_towerTeam2 != null)
        {
            foreach (var tower in _towerTeam2)
            {
                var objectHealth = tower.GetComponent<ObjectHealth>();
                if (objectHealth != null)
                {
                    objectHealth.OnDeath -= () => OnTowerDestroyed(2);
                }
            }
        }
    }


    [ClientRpc]
    private void RpcTeleportPlayer(GameObject player, Vector3 position, Quaternion rotation)
    {
        player.transform.SetPositionAndRotation(position, rotation);
    }

    [ClientRpc]
    private void RpcCloseRoomOnClients()
    {
        StartCoroutine(CloseRoomOnClientAndLoadMainMenu());
    }

    private IEnumerator CloseRoomOnClientAndLoadMainMenu()
    {
        yield return StartCoroutine(CloseRoomJob());
        SceneManager.LoadScene("MainMenu");
    }

    private IEnumerator HandleTeamsAndSpawns(List<Transform> spawnPoints)
    {
        yield return StartCoroutine(SplitTeams(spawnPoints));
        foreach (var playerSettings in _players)
        {
            int spawnIndex = playerSettings.NetworkSettings.TeamIndex - 1;
            if (_spawnPoints != null && spawnIndex >= 0 && spawnIndex < _spawnPoints.Count)
            {
                Transform spawnPoint = _spawnPoints[spawnIndex];
                playerSettings.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
            }
        }

        yield return StartCoroutine(SavePositionsAndAssignLayers());
    }

    private IEnumerator CloseJob()
    {
        while (_lifeTime > 0)
        {
            _lifeTime -= Time.deltaTime;
            yield return null;
        }

        StartCoroutine(CloseRoomJob());
    }
}