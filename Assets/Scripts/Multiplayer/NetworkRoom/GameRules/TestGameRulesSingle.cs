using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TestGameRulesSingle : GameRules
{
    [Header("Game Settings")]
    [SerializeField] private float _lifeTime = 10f;
    [SerializeField] private bool isRemoveRoom = true;

    private int _teamMaxScore = 1;
    private int _team1Score = 0;
    private int _team2Score = 0;

    public override void GameStartServer(HeroSpawnManager spawnPoints)
    {
        StartCoroutine(HandleTeamsAndSpawns(spawnPoints));
    }

    protected override void GameStartClient()
    {
        _preparationAreaManager?.PreparationAreasDisable(5f);

        if (isLocalPlayer && TryGetComponent(out HeroComponent hero))
        {
            LevelCharacterManager.Instance.SetHero(hero);
        }
    }

    protected override void OnTowerDied(Object tower)
    {
        if (!isServer) return;

        if (!tower.DestroyOnDeath)
        {
            StartCoroutine(HandleTowerDestructionLogic(tower));
            return;
        }

        AddScorePointFromTower(tower.IndexTeam == 1 ? 2 : 1);
    }

    private IEnumerator HandleTowerDestructionLogic(Object destroyedTower)
    {
        yield return new WaitForSeconds(1f);

        int winningTeam = destroyedTower.IndexTeam == 1 ? 2 : 1;
        AddScorePointFromTower(winningTeam);

        var allTowers = GameObject.FindObjectsOfType<Object>().Where(obj => obj.ObjectHealth != null && !obj.DestroyOnDeath);
        foreach (var tower in allTowers)
        {
            tower.ObjectHealth.ServerSetCurrentHealth(tower.ObjectHealth.MaxValue);
            tower.Live = true;
        }
    }

    private void AddScorePointFromTower(int winningTeam)
    {
        if (winningTeam == 1)
        {
            _team1Score++;
            RpcSetSource(1, _team1Score);
        }
        else if (winningTeam == 2)
        {
            _team2Score++;
            RpcSetSource(2, _team2Score);
        }

        if (_team1Score >= _teamMaxScore || _team2Score >= _teamMaxScore)
        {
            RpcShowWinner(_team1Score > _team2Score ? 1 : 2);
            AfterEndGame();
            EndGame();
        }
    }

    private void AfterEndGame()
    {
        if (!isServer) return;
        var bottleManager = BottleUserManager.Instance;
        var levelManager = LevelCharacterManager.Instance;

        int experience = 1000;
        float bottleVolume = 1000;

        foreach (var player in _players)
{
    TargetApplyRewards(player.connectionToClient, experience, bottleVolume);
}
    }
    
    private IEnumerator HandleTeamsAndSpawns(HeroSpawnManager spawnPoints)
    {
        yield return StartCoroutine(SplitTeams(spawnPoints));

        foreach (var player in _players)
        {
            int spawnIndex = player.NetworkSettings.TeamIndex - 1;
            if (_spawnPoints != null)
            {
                player.transform.SetPositionAndRotation(_spawnPoints.GetRandomPoint(spawnIndex), _spawnPoints.GetRotate(spawnIndex));
            }
        }

        yield return StartCoroutine(SavePositionsAndAssignLayers());
    }

    protected override void UnsubscribeFromAllEvents()
    {
        if (!isServer) return;

        foreach (var tower in GameObject.FindObjectsOfType<Object>().Where(obj => obj.IsTower))
        {
            tower.Died -= OnTowerDied;
        }
    }

    [TargetRpc]
    private void TargetApplyRewards(NetworkConnectionToClient connection, int experience, float bottleVolume)
    {
        BottleUserManager.Instance.AddBottleVolume(bottleVolume);

        if (LevelCharacterManager.Instance.TryGetCurrentHero(out var hero))
        {
            LevelCharacterManager.Instance.AddExperience(experience);
            Debug.Log("[Client] Experience applied to selected hero.");
        }
        else
        {
            Debug.LogWarning("[Client] No hero set in LevelCharacterManager. Experience not applied.");
        }
    }

    protected override void OnPlayerDied(Character character)
    {
        throw new System.NotImplementedException();
    }

}
