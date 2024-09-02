using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.UIElements;

public class SpawnComponent : NetworkBehaviour
{
    [SerializeField] private MinionComponent unit;
    
    private List<MinionComponent> _units = new List<MinionComponent>();

    public List<MinionComponent> Units => _units;

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
            
        _units.Add(contollableMinion);
            
        var position = _units.Count + 1 / Positions.unitInGroupPositions.Count;

        controllable.transform.position = (Vector2) parent.transform.position + Positions.unitInGroupPositions[position];
        
        controllable.GetComponent<MinionComponent>().SetParent(parent);
        
        NetworkServer.Spawn(controllable.gameObject , parent);
    }
	public void SpawnUnit(Transform transform)
	{
		var controllable = Instantiate(unit, transform.position, Quaternion.identity);
		var contollableMinion = controllable.GetComponent<MinionComponent>();

		_units.Add(contollableMinion);

		controllable.GetComponent<MinionComponent>().SetParent(gameObject);

		NetworkServer.Spawn(controllable.gameObject, gameObject);
	}


	public void RemoveUnit()
    {
        Destroy(_units.Last().gameObject);
        _units.Remove(_units.Last());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z) && GetComponent<SelectComponent>().IsSelect)
        {
			Cmd_SpawnUnit(gameObject);
        }
        
        if (Input.GetKeyDown(KeyCode.X) && GetComponent<SelectComponent>().IsSelect)
        {
           RemoveUnit();
        }
    }
}
