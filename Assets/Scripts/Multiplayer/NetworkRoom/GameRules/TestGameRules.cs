using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TestGameRules : GameRules
{
    [Header("Game Settings")]
    [SerializeField] private float _lifeTime = 10f;
    [SerializeField] private bool isRemoveRoom = true;

    [Header("Team Settings")]
    [SerializeField] private int maxScore = 2;
    [SerializeField] private int experiencePerWin = 6;
    [SerializeField] private int experiencePerLoss = 2;
    [SerializeField] private float bottleVolumePerWin = 1f / 3f;

    private TeamsPanel _teams;
    private int[] teamDeaths = new int[3];
    private int team1Score = 0;
    private int team2Score = 0;

    public override void GameStartServer(List<Transform> spawnPoints)
    {
        StartCoroutine(HandleTeamsAndSpawns(spawnPoints));

        foreach (var playerSettings in _players)
        {
            var health = playerSettings.NetworkSettings.CachedHealth;
            if (health != null)
            {
                health.Died += () => OnPlayerDeath(playerSettings.gameObject);
                health.Died += () => ResetPlayerState(playerSettings);
            }

            var runeComponent = playerSettings.GetComponent<RuneComponent>();
            if (runeComponent != null)
            {
                if (health != null)
                {
                    health.Died += runeComponent.ResetValue;
                }
            }
        }
    }

    protected override void GameStartClient()
    {
        _teams = FindObjectOfType<TeamsPanel>();

        foreach (var playerSettings in _players)
        {
            if (playerSettings.NetworkSettings.TeamIndex == 1)
            {
                _teams.AddInFirstTeam(playerSettings.GetComponent<Character>());
            }
            else
            {
                _teams.AddInSecondTeam(playerSettings.GetComponent<Character>());
            }
        }
    }

    public void OnPlayerDeath(GameObject player)
    {
        var playerSettings = _players.Find(p => p.gameObject == player);
        if (playerSettings == null || playerSettings.NetworkSettings.TeamIndex < 1 || playerSettings.NetworkSettings.TeamIndex > 2) return;

        teamDeaths[playerSettings.NetworkSettings.TeamIndex]++;
        CheckForRoundEnd();
    }

    private void CancelActiveSkills(Character playerSettings)
    {
        var skills = playerSettings.Abilities.Abilities;
        foreach (var skill in skills)
        {
            skill.RpcCancelActiveSkill();
            skill.RpcResetSkillState();
        }
    }

    private void CheckForRoundEnd()
    {
        if (teamDeaths[1] == GetTeamCount(1) || teamDeaths[2] == GetTeamCount(2))
        {
            team2Score += teamDeaths[1] == GetTeamCount(1) ? 1 : 0;
            team1Score += teamDeaths[2] == GetTeamCount(2) ? 1 : 0;

            Debug.Log($"Round Over! Team 1 Score: {team1Score}, Team 2 Score: {team2Score}");
            if (team1Score >= maxScore || team2Score >= maxScore)
            {
                EndGame();
            }
            else
            {
                RestartRound();
            }
        }
    }

    private void EndGame()
    {
        if (!isServer) return;

        GameMode currentMode = ServerManager.Instance.CurrentGameMode;
        bool isMaxLevel = User.LevelCharacterManager.Instance.GetCurrentLevel() >= User.LevelCharacterManager.Instance.MaxLevel;
        bool isVictory = team1Score >= maxScore;

        switch (currentMode)
        {
            case GameMode.GM1vs1MaximumMode:
                if (isVictory)
                {
                    if (isMaxLevel)
                    {
                        User.BottleUserManager.Instance.AddBottleVolume(bottleVolumePerWin);
                    }
                    else
                    {
                        User.LevelCharacterManager.Instance.AddExperience(experiencePerWin);
                        User.BottleUserManager.Instance.AddBottleVolume(bottleVolumePerWin);
                    }
                }
                else if (!isMaxLevel)
                {
                    User.LevelCharacterManager.Instance.AddExperience(experiencePerLoss);
                }
                break;

            default:
                if (isVictory)
                {
                    if (isMaxLevel)
                    {
                        User.BottleUserManager.Instance.AddBottleVolume(bottleVolumePerWin);
                    }
                    else
                    {
                        User.LevelCharacterManager.Instance.AddExperience(experiencePerLoss);
                    }
                }
                break;
        }

        RpcCloseRoomOnClients();
        StartCoroutine(CloseJob());
    }

    private void RestartRound()
    {
        teamDeaths[1] = 0;
        teamDeaths[2] = 0;

        if (isServer)
        {
            List<NetworkIdentity> objectsToRemove = new List<NetworkIdentity>();

            foreach (var networkIdentity in NetworkServer.spawned.Values)
            {
                bool isPlayer = _players.Exists(player => player.gameObject == networkIdentity.gameObject);
                bool isTestGameRules = networkIdentity.GetComponent<TestGameRules>() != null;
                bool isUser = networkIdentity.GetComponent<User>() != null;

                if (networkIdentity != null && !isPlayer && !isTestGameRules && !isUser)
                {
                    objectsToRemove.Add(networkIdentity);
                }
            }

            foreach (var networkIdentity in objectsToRemove)
            {
                if (networkIdentity != null && networkIdentity.isServer)
                {
                    NetworkServer.Destroy(networkIdentity.gameObject);
                }
            }
        }

        foreach (var playerSettings in _players)
        {
            ResetPlayerState(playerSettings);

            int spawnIndex = playerSettings.NetworkSettings.TeamIndex - 1;
            if (_spawnPoints != null && spawnIndex >= 0 && spawnIndex < _spawnPoints.Count)
            {
                Transform spawnPoint = _spawnPoints[spawnIndex];
                RpcTeleportPlayer(playerSettings.gameObject, spawnPoint.position, spawnPoint.rotation);
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

    private int GetTeamCount(int teamIndex)
    {
        int count = 0;
        foreach (var playerSettings in _players)
        {
            if (playerSettings.NetworkSettings.TeamIndex == teamIndex)
            {
                count++;
            }
        }
        return count;
    }

    protected override void UnsubscribeFromAllEvents()
    {
        foreach (var playerSettings in _players)
        {
            var health = playerSettings.NetworkSettings.CachedHealth;
            if (health != null)
            {
                health.Died -= () => OnPlayerDeath(playerSettings.gameObject);
                health.Died -= () => ResetPlayerState(playerSettings);
            }

            var runeComponent = playerSettings.GetComponent<RuneComponent>();
            if (runeComponent != null && health != null)
            {
                health.Died -= runeComponent.ResetValue;
            }
        }

        if (isServer)
        {
            List<NetworkIdentity> objectsToRemove = new List<NetworkIdentity>();

            foreach (var networkIdentity in NetworkServer.spawned.Values)
            {
                var healthComponent = networkIdentity.GetComponent<Health>();
                if (healthComponent != null)
                {
                    healthComponent.Died -= () => OnPlayerDeath(networkIdentity.gameObject);
                }

                bool isPlayer = _players.Exists(player => player.gameObject == networkIdentity.gameObject);
                if (!isPlayer)
                {
                    objectsToRemove.Add(networkIdentity);
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
        yield return StartCoroutine(CloseJob());

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
