using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using TMPro.EditorUtilities;
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
        var temp = _minionPrefs[index];

        var contollableMinion = Instantiate(temp, position, Quaternion.identity);
		contollableMinion.Initialize();

		SceneManager.MoveGameObjectToScene(contollableMinion.gameObject, _hero.NetworkSettings.MyRoom);

        NetworkServer.Spawn(contollableMinion.gameObject, connectionToClient);

        AddUnit(contollableMinion);
    }

    public void AddUnit(MinionComponent minion)
    {
        _units.Add(minion);
        UnitAdded?.Invoke(minion);

        minion.Destroyed += OnUnitDestroyed;
        minion.Intercepted += OnUnitDestroyed;

        ClientRpcUnitAdded(minion.gameObject);
    }

    private void OnUnitDestroyed(MinionComponent minion)
    {
        _units.Remove(minion);
        UnitRemoved?.Invoke();

        minion.Destroyed -= OnUnitDestroyed;
        minion.Intercepted -= OnUnitDestroyed;

        ClientRpcOnUnitDestroyed(minion.gameObject);
    }

    [Command]
    public void CmdSpawnUnit(int index, Vector3 position)
    {
        SpawnUnit(index, position);
    }

    [ClientRpc]
    private void ClientRpcUnitAdded(GameObject minion)
    {
        var minionTemp = minion.GetComponent<MinionComponent>();
        _units.Add(minionTemp);
        UnitAdded?.Invoke(minionTemp);
    }

    [ClientRpc]
    private void ClientRpcOnUnitDestroyed(GameObject minion)
    {
        if (minion != null)
        {
            _units.Remove(minion.GetComponent<MinionComponent>());
        }
        else
        {
            for (int i = 0; i < _units.Count; i++)
            {
                if(_units[i] == null)
                {
                    _units.RemoveAt(i);
                    i--;
                }
            }
        }

        UnitRemoved?.Invoke();
    }
}
