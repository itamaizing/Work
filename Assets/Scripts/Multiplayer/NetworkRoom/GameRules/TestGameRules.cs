using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestGameRules : GameRules
{
    [SerializeField] private float _lifeTime = 10f;
    [SerializeField] private bool isRemoveRoom = true;

    private TeamsPanel _teams;
    private int[] teamDeaths = new int[3];
    private int team1Score = 0;
    private int team2Score = 0;

    public override void GameStartServer(List<Transform> spawnPoints)
    {
        StartCoroutine(HandleTeamsAndSpawns(spawnPoints));

        foreach (var playerSettings in _playersSettings)
        {
            var health = playerSettings.NetworkSettings.CachedHealth;
            if (health != null)
            {
                health.Died += () => OnPlayerDeath(playerSettings.gameObject);
            }
        }

        if (isServer && isRemoveRoom)
            StartCoroutine(CloseJob());
    }

    protected override void GameStartClient()
    {
        _teams = FindObjectOfType<TeamsPanel>();

        foreach (var playerSettings in _playersSettings)
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
        var playerSettings = _playersSettings.Find(p => p.gameObject == player);
        if (playerSettings == null || playerSettings.NetworkSettings.TeamIndex < 1 || playerSettings.NetworkSettings.TeamIndex > 2) return;

        teamDeaths[playerSettings.NetworkSettings.TeamIndex]++;
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
        foreach (var playerSettings in _playersSettings)
        {
            if (playerSettings.NetworkSettings.TeamIndex == teamIndex)
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

        foreach (var playerSettings in _playersSettings)
        {
            var health = playerSettings.NetworkSettings.CachedHealth;
            health?.ResetValue();

            int spawnIndex = playerSettings.NetworkSettings.TeamIndex - 1;
            if (_spawnPoints != null && spawnIndex >= 0 && spawnIndex < _spawnPoints.Count)
            {
                Transform spawnPoint = _spawnPoints[spawnIndex];
                playerSettings.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
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
