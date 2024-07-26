using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class SpawnComponent : NetworkBehaviour
{
    [SerializeField] private GameObject unit;
    
    private readonly List<UnitComponent> _units = new();

    private void SpawnUnit(GameObject parent)
    {
        if (!isOwned) return;
        
        Cmd_SpawnUnit(parent);
    }
    
    [Command]
    public void Cmd_SpawnUnit(GameObject parent)
    {
        var controllable = Instantiate(unit);
        var contollableMinion = controllable.GetComponent<UnitComponent>();
            
        _units.Add(contollableMinion);
            
        var position = _units.Count + 1 / Positions.unitInGroupPositions.Count;

        controllable.transform.position = (Vector2) parent.transform.position + Positions.unitInGroupPositions[position];
        
        contollableMinion.SetParent(parent);
        
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
            SpawnUnit(this.gameObject);
        }
        
        if (Input.GetKeyDown(KeyCode.X))
        {
           RemoveUnit();
        }
    }
}
