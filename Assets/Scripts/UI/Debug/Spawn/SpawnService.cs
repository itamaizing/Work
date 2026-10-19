using Mirror;
using UnityEngine;

namespace Game.Debug
{
    public class SpawnService : NetworkBehaviour
    {
        private MPNetworkManager _mpManager;

        private void OnEnable()
        {
            _mpManager = MPNetworkManager.Instance;
        }

        [Command(requiresAuthority = false)]
        public void CmdSpawn(string prefab, Vector3 position)
        {
            Spawn(prefab, position);
        }

        [Command(requiresAuthority = false)]
        public void CmdSpawn(string prefab)
        {
            Spawn(prefab, new Vector3(0, 0, 0));
        }

        public void Spawn(string prefabName, Vector3 position)
        {
            var prefabs = _mpManager.spawnPrefabs;

            int prefabIndex = -1;
            for (int i = 0; i < prefabs.Count; i++)
            {
                if (prefabs[i] != null && prefabs[i].name == prefabName)
                {
                    prefabIndex = i;
                    break;
                }
            }
            GameObject prefabToSpawn = prefabs[prefabIndex];
            GameObject spawnedObject = Instantiate(prefabToSpawn, position, Quaternion.identity);
            NetworkServer.Spawn(spawnedObject);
            
            if (spawnedObject.TryGetComponent<HeroComponent>(out var hero))
                hero.Initialize();
        }
    }
}