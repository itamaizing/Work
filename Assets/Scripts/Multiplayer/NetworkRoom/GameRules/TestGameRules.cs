using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestGameRules : GameRules
{
    [SerializeField] private float _lifeTime = 10f;
    [SerializeField] private bool isRemoveRoom = true;

    private TeamsPanel _teams;
    private SpawnPointsContainer _spawnPointsContainer;
    private List<Transform> _spawnPoints;

    private int[] teamDeaths = new int[3];
    private int team1Score = 0;
    private int team2Score = 0;

    public override void GameStartServer(List<Transform> spawnPoints)
    {
        _spawnPointsContainer = FindObjectOfType<SpawnPointsContainer>();
        if (_spawnPointsContainer != null)
        {
            _spawnPoints = _spawnPointsContainer.GetSpawnPoints();
        }

        StartCoroutine(HandleTeamsAndSpawns(_spawnPoints));

        if (isServer && isRemoveRoom)
            StartCoroutine(CloseJob());
    }

    protected override void GameStartClient()
    {
        _teams = FindObjectOfType<TeamsPanel>();

        for (int i = 0; i < _players.Count; i++)
        {
            if (_players[i].GetComponent<UserNetworkSettings>().TeamIndex == 1)
            {
                _teams.AddInFirstTeam(_players[i].GetComponent<Character>());
            }
            else
            {
                _teams.AddInSecondTeam(_players[i].GetComponent<Character>());
            }
        }
    }

    public void OnPlayerDeath(GameObject player)
    {
        var playerSettings = player.GetComponent<UserNetworkSettings>();
        if (playerSettings == null || playerSettings.TeamIndex < 1 || playerSettings.TeamIndex > 2) return;

        teamDeaths[playerSettings.TeamIndex]++;

        CheckForRoundEnd();
    }

    private void CheckForRoundEnd()
    {
        if (teamDeaths[1] == GetTeamCount(1) || teamDeaths[2] == GetTeamCount(2))
        {
            team2Score += teamDeaths[1] == GetTeamCount(1) ? 1 : 0;
            team1Score += teamDeaths[2] == GetTeamCount(2) ? 1 : 0;

            Debug.Log($"Round Over! Team 1 Score: {team1Score}, Team 2 Score: {team2Score}");
            RestartRound();
        }
    }

    private int GetTeamCount(int teamIndex)
    {
        int count = 0;
        foreach (var player in _players)
        {
            var playerSettings = player.GetComponent<UserNetworkSettings>();
            if (playerSettings.TeamIndex == teamIndex)
            {
                count++;
            }
        }
        return count;
    }

    private void RestartRound()
    {
        teamDeaths[1] = 0;
        teamDeaths[2] = 0;

        foreach (var player in _players)
        {
            var health = player.GetComponent<Health>();
            var playerSettings = player.GetComponent<UserNetworkSettings>();

            health?.ResetValue();

            int spawnIndex = playerSettings.TeamIndex - 1;
            if (_spawnPoints != null && spawnIndex >= 0 && spawnIndex < _spawnPoints.Count)
            {
                Transform spawnPoint = _spawnPoints[spawnIndex];
                player.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
            }
        }
    }

    private IEnumerator HandleTeamsAndSpawns(List<Transform> spawnPoints)
    {
        yield return StartCoroutine(SplitTeams(spawnPoints));
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
