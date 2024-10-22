using System.Collections.Generic;
using UnityEngine;

public class SpawnPointsContainer : MonoBehaviour
{
    [SerializeField] private List<Transform> spawnPoints;

    public List<Transform> GetSpawnPoints()
    {
        return spawnPoints;
    }
}
