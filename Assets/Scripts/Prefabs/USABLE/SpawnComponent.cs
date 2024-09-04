using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class SpawnComponent : NetworkBehaviour
{
    [SerializeField] private MinionComponent unit;
    
    private readonly List<MinionComponent> _units = new();

    private void SpawnUnit(GameObject parent)
    {
        if (!isOwned) return;
        
        Cmd_SpawnUnit(parent);
    }
    
    [Command]
    public void Cmd_SpawnUnit(Transform transform)
    {
		SpawnUnit(transform);
    }

	[Command]
	public void Cmd_SpawnUnit(GameObject parent)
	{
		SpawnUnit(parent);
	}

	public void SpawnUnit(GameObject parent)
    {
        var controllable = Instantiate(unit);
        var contollableMinion = controllable.GetComponent<MinionComponent>();
        contollableMinion.Initialize();
        var user = GetComponent<UserNetworkSettings>();
        
        SceneManager.MoveGameObjectToScene(controllable, user.MyRoom);
            
        _units.Add(contollableMinion);
            
        var position = _units.Count + 1 / Positions.unitInGroupPositions.Count;

        controllable.transform.position = (Vector2) parent.transform.position + Positions.unitInGroupPositions[position];
        
        NetworkServer.Spawn(controllable , connectionToClient);
    }


    private void RemoveUnit()
    {
        Destroy(_units.Last().gameObject);
        _units.Remove(_units.Last());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
			Cmd_SpawnUnit(gameObject);
        }
        
        if (Input.GetKeyDown(KeyCode.X))
        {
            RemoveUnit();
        }
    }
}
