using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        if (isServer && isRemoveRoom)
            StartCoroutine(CloseJob());
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
            RestartRound();
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

    [ClientRpc]
    private void RpcTeleportPlayer(GameObject player, Vector3 position, Quaternion rotation)
    {
        player.transform.SetPositionAndRotation(position, rotation);
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

                if (networkIdentity != null && !isPlayer && !isTestGameRules)
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

        //CancelActiveSkills(playerSettings);

        var characterState = playerSettings.CharacterState;
        if (characterState != null)
        {
            var statesCopy = new List<AbstractCharacterState>(characterState.CurrentStates);
            foreach (var state in statesCopy)
            {
                characterState.RemoveState(state.State);
            }
        }

        var energy = playerSettings.Resources.First(o => o.Type == ResourceType.Mana);
        if (energy != null)
        {
            energy.ResetValue();
        }
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