using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class SpawnComponent : NetworkBehaviour
{
    [SerializeField] private MinionComponent unit;
    
    
    private List<MinionComponent> _units = new List<MinionComponent>();

    public void SpawnUnit(GameObject parent)
    {
        Cmd_SpawnUnit(parent);
    }
    
    [Command]
    public void Cmd_SpawnUnit(GameObject parent)
    {
        var controllable = Instantiate(unit.gameObject);
        var contollableMinion = unit.GetComponent<MinionComponent>();
        var parentHC = parent.GetComponent<HeroComponent>();
            
        _units.Add(contollableMinion);
            
        var position = _units.Count + 1 / Positions.unitInGroupPositions.Count;

        controllable.transform.position = (Vector2) parent.transform.position + Positions.unitInGroupPositions[position];

        NetworkServer.Spawn(controllable);
    }
    

    public void RemoveUnit()
    {
        Destroy(_units.Last().gameObject);
        _units.Remove(_units.Last());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z) && GetComponent<SelectComponent>().IsSelect )
        {
            SpawnUnit(this.gameObject);
        }
        
        if (Input.GetKeyDown(KeyCode.X) && GetComponent<SelectComponent>().IsSelect)
        {
           RemoveUnit();
        }
    }
}
