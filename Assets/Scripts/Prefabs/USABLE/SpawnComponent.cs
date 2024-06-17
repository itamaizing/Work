using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpawnComponent : MonoBehaviour
{
    [SerializeField] private MinionComponent unit;
    
    private List<MinionComponent> _units = new List<MinionComponent>();

    public void SpawnUnit(HeroComponent parent)
    {
            var controllable = Instantiate(unit);
            
            _units.Add(controllable);
            
            var position = _units.Count + 1 / Positions.unitInGroupPositions.Count;
            Debug.Log("spawnComp position - " + position);

            controllable.transform.position = (Vector2) parent.transform.position + Positions.unitInGroupPositions[position];
            controllable.SetMinion(parent);
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
            SpawnUnit(GetComponent<HeroComponent>());
        }
        
        if (Input.GetKeyDown(KeyCode.X) && GetComponent<SelectComponent>().IsSelect)
        {
           RemoveUnit();
        }
    }
}
