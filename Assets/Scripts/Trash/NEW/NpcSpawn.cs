using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NpcSpawn : MonoBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] private List<Transform> _spawnPointsEnemyNpc;
    [SerializeField] private List<Transform> _spawnPointsAlliesNpc;

    [Header("Prefabs")]
    [SerializeField] private Character _enemyNpcPrefab;
    [SerializeField] private Character _alliesNpcPrefab;

    private const byte NpcTeamIndex = 3;

    private readonly List<GameObject> _spawnedNpcs = new();

    public IReadOnlyList<GameObject> SpawnedNpcs => _spawnedNpcs;

    public void SpawnAllNpc(Scene roomScene)
    {
        if (!NetworkServer.active) return;
        
        SpawnNpcGroup(_spawnPointsEnemyNpc, _enemyNpcPrefab, roomScene);
        SpawnNpcGroup(_spawnPointsAlliesNpc, _alliesNpcPrefab, roomScene);
    }

    private void SpawnNpcGroup(List<Transform> points, Character prefab, Scene roomScene)
    {
        if (prefab == null) return;

        foreach (Transform point in points)
        {
            if (point == null) continue;

            Character npc = Instantiate(prefab, point.position, point.rotation);
            SceneManager.MoveGameObjectToScene(npc.gameObject, roomScene);

            NetworkServer.Spawn(npc.gameObject);

            npc.NetworkSettings.TeamIndex = NpcTeamIndex;

            if (npc.TryGetComponent(out UnitLayerSync layerSync)) layerSync.TeamIndex = NpcTeamIndex;
            AddNpcToPlayerLists(npc.gameObject);
            _spawnedNpcs.Add(npc.gameObject);
        }
    }

    private void AddNpcToPlayerLists(GameObject npc)
    {
        foreach (UserNetworkSettings settings in FindObjectsOfType<UserNetworkSettings>())
        {
            if (settings == null) continue;
            if (settings.connectionToClient == null) continue;
            if (!settings.Players.Contains(npc)) settings.Players.Add(npc);
        }
    }

    public void DestroyAllNpc()
    {
        if (!NetworkServer.active) return;

        foreach (var npc in _spawnedNpcs)
        {
            if (npc != null) NetworkServer.Destroy(npc);
        }

        _spawnedNpcs.Clear();
    }
}