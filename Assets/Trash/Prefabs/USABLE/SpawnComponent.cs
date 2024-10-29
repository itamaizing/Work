using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SpawnComponent : NetworkBehaviour
{
    [SerializeField] private Character _hero;
    [SerializeField] private List<MinionComponent> _minionPrefs;

    private readonly List<MinionComponent> _units = new();

    public List<MinionComponent> Units => _units;

    public event Action<MinionComponent> UnitAdded;
    public event Action UnitRemoved;

    public void SpawnUnit(int index, Vector3 position)
    {
        if (index < 0 || index >= _minionPrefs.Count)
        {
            Debug.LogError($"Index {index} is out of bounds for spawning units.");
            return;
        }

        var temp = _minionPrefs[index];

        if (temp == null)
        {
            Debug.LogError("Minion prefab is null.");
            return;
        }

        var controllableMinion = Instantiate(temp, position, Quaternion.identity);
        controllableMinion.Initialize();

        if (_hero == null || _hero.NetworkSettings == null)
        {
            Debug.LogError("Hero or NetworkSettings is null. Cannot move unit to scene.");
            return;
        }

        SceneManager.MoveGameObjectToScene(controllableMinion.gameObject, _hero.NetworkSettings.MyRoom);

        if (connectionToClient == null)
        {
            Debug.LogError("Connection to client is null. Cannot spawn unit.");
            Destroy(controllableMinion.gameObject);
            return;
        }

        NetworkServer.Spawn(controllableMinion.gameObject, connectionToClient);

        AddUnit(controllableMinion);
    }

    public void AddUnit(MinionComponent minion)
    {
        if (minion == null)
        {
            Debug.LogError("Attempted to add a null minion to the units list.");
            return;
        }

        _units.Add(minion);
        UnitAdded?.Invoke(minion);

        Debug.Log($"Unit {minion.name} added. Total units: {_units.Count}");

        minion.Destroyed += OnUnitDestroyed;
        minion.Intercepted += OnUnitDestroyed;

        ClientRpcUnitAdded(minion.gameObject);
    }

    private void OnUnitDestroyed(MinionComponent minion)
    {
        if (minion == null)
        {
            Debug.LogWarning("Minion is null in OnUnitDestroyed.");
            return;
        }

        if (_units.Contains(minion))
        {
            _units.Remove(minion);
            UnitRemoved?.Invoke();

            minion.Destroyed -= OnUnitDestroyed;
            minion.Intercepted -= OnUnitDestroyed;

            Debug.Log($"Unit {minion.name} destroyed. Total units left: {_units.Count}");

            ClientRpcOnUnitDestroyed(minion.gameObject);
        }
        else
        {
            Debug.LogWarning("Minion not found in units list.");
        }
    }

    [Command]
    public void CmdSpawnUnit(int index, Vector3 position)
    {
        SpawnUnit(index, position);
    }

    [ClientRpc]
    private void ClientRpcUnitAdded(GameObject minion)
    {
        if (minion == null)
        {
            Debug.LogWarning("Minion is null in ClientRpcUnitAdded.");
            return;
        }

        var minionTemp = minion.GetComponent<MinionComponent>();
        if (minionTemp == null)
        {
            Debug.LogError("MinionComponent is missing on spawned object.");
            return;
        }

        _units.Add(minionTemp);
        UnitAdded?.Invoke(minionTemp);

        Debug.Log($"Unit {minionTemp.name} added on client. Total units: {_units.Count}");
    }

    [ClientRpc]
    private void ClientRpcOnUnitDestroyed(GameObject minion)
    {
        if (minion != null)
        {
            var minionComponent = minion.GetComponent<MinionComponent>();
            if (minionComponent != null)
            {
                _units.Remove(minionComponent);
                Debug.Log($"Unit {minionComponent.name} removed on client. Total units left: {_units.Count}");
            }
            else
            {
                Debug.LogWarning("MinionComponent is null on destroyed object.");
            }
        }
        else
        {
            Debug.LogWarning("Minion is null in ClientRpcOnUnitDestroyed, cleaning up null units.");
            for (int i = 0; i < _units.Count; i++)
            {
                if (_units[i] == null)
                {
                    _units.RemoveAt(i);
                    i--;
                }
            }
        }

        UnitRemoved?.Invoke();
    }
}