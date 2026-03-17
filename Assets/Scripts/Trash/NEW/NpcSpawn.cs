using Mirror;
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

    private int EnemyLayer = LayerMask.NameToLayer("Enemy");
    private int AlliesLayer = LayerMask.NameToLayer("Allies");

    private readonly List<GameObject> _spawnedNpcs = new();

    public IReadOnlyList<GameObject> SpawnedNpcs => _spawnedNpcs;

    public void SpawnAllNpc(Scene roomScene)
    {
        if (!NetworkServer.active) return;

        SpawnNpcGroup(_spawnPointsEnemyNpc, _enemyNpcPrefab, roomScene, EnemyLayer);
        //SpawnNpcGroup(_spawnPointsAlliesNpc, _alliesNpcPrefab, roomScene, AlliesLayer);
    }

    private void SpawnNpcGroup(List<Transform> points, Character prefab, Scene roomScene, LayerMask layer)
    {
        if (prefab == null) return;

        foreach (var point in points)
        {
            if (point == null) continue;

            var npc = Instantiate(prefab, point.position, point.rotation);

           npc.gameObject.layer = layer;

            npc.NetworkSettings.TeamIndex = NpcTeamIndex;

            SceneManager.MoveGameObjectToScene(npc.gameObject, roomScene);
            NetworkServer.Spawn(npc.gameObject);

            _spawnedNpcs.Add(npc.gameObject);
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