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
    public event Action<MinionComponent> UnitRemoved;

	public void SpawnUnit(int index, Vector3 position)
	{
        var temp = _minionPrefs[index];

        var contollableMinion = Instantiate(temp, position, Quaternion.identity);
		contollableMinion.Initialize();

		SceneManager.MoveGameObjectToScene(contollableMinion.gameObject, _hero.NetworkSettings.MyRoom);

        NetworkServer.Spawn(contollableMinion.gameObject, connectionToClient);

        AddUnit(contollableMinion);
        TargetRpcUnitAdded(contollableMinion.gameObject);

        contollableMinion.Destroyed += OnUnitDestroyed;
    }

    private void AddUnit(MinionComponent minion)
    {
        _units.Add(minion);
        UnitAdded?.Invoke(minion);
    }

	private void OnUnitDestroyed(MinionComponent minion)
    {
        _units.Remove(minion);
        UnitRemoved?.Invoke(minion);

        minion.Destroyed -= OnUnitDestroyed;

        if (isServer)
        {
            TargetRpcOnUnitDestroyed(minion.gameObject);
        }
    }

    [Command]
    public void CmdSpawnUnit(int index, Vector3 position)
    {
        SpawnUnit(index, position);
    }

    [TargetRpc]
    private void TargetRpcUnitAdded(GameObject minion)
    {
        AddUnit(minion.GetComponent<MinionComponent>());
    }

    [TargetRpc]
    private void TargetRpcOnUnitDestroyed(GameObject minion)
    {
        OnUnitDestroyed(minion.GetComponent<MinionComponent>());
    }
}
