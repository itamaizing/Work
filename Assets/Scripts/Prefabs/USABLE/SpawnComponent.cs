using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpawnComponent : MonoBehaviour
{
    [SerializeField] private MinionComponent unit;
    
    private List<MinionComponent> _units = new List<MinionComponent>();

    public List<Vector2> spawnPositions = new List<Vector2>()
    {
        new Vector2(0, 2),
        new Vector2(2, 0),
        new Vector2(2, 2),

        new Vector2(0, -2),
        new Vector2(-2, 0),
        new Vector2(-2, -2),
    };

    public void SpawnUnit(HeroComponent parent)
    {
            var controllable = Instantiate(unit);
            controllable.transform.position = (Vector2) parent.transform.position + spawnPositions[_units.Count];
            controllable.SetMinion(parent);
            controllable.GetComponent<MoveComponent>().SetOffset(spawnPositions[_units.Count]);
            
            _units.Add(controllable);
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
