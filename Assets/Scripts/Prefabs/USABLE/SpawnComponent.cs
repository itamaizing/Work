using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class SpawnComponent : NetworkBehaviour
{
    [SerializeField] private GameObject unit;
    
    private List<MinionComponent> _units = new List<MinionComponent>();

    public void SpawnUnit(GameObject parent)
    {
        Cmd_SpawnUnit(parent);
    }
    
    [Command]
    public void Cmd_SpawnUnit(GameObject parent)
    {
        var controllable = Instantiate(unit);
        var contollableMinion = controllable.GetComponent<MinionComponent>();
            
        _units.Add(contollableMinion);
            
        var position = _units.Count + 1 / Positions.unitInGroupPositions.Count;

        controllable.transform.position = (Vector2) parent.transform.position + Positions.unitInGroupPositions[position];
        
        controllable.GetComponent<MinionComponent>().SetParent(parent);
        
        NetworkServer.Spawn(controllable , parent);
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
            SpawnUnit(this.gameObject);
        }
        
        if (Input.GetKeyDown(KeyCode.X) && GetComponent<SelectComponent>().IsSelect)
        {
           RemoveUnit();
        }
    }
}
