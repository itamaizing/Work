using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SelectManager : MonoBehaviour
{
    [SerializeField] private BoxSelector _selectorPrefab;
    
    [SerializeField] private LayerMask allies;
    [SerializeField] private LayerMask enemies;

    private bool isControllable = false;
    
    private static SelectManager instance; 
    public static SelectManager Instance => instance;

    private Character _contoller;
    private bool _canControll = false;
    
    public List<Character> selectedControllableUnits;

    private int currentUnitNumber = 0;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }
        
        selectedControllableUnits = new List<Character>();
        _selectorPrefab.gameObject.SetActive(false);
    }
    
    private void LateUpdate()
    {
        if (Input.GetMouseButtonDown(0) && Input.GetKey(KeyCode.LeftShift))
        {
            _selectorPrefab.gameObject.SetActive(true);
            _selectorPrefab.StartDraw();
        }

        if (Input.GetMouseButton(0) && Input.GetKey(KeyCode.LeftShift))
        {
            _selectorPrefab.Draw();
        }

        if (Input.GetMouseButtonUp(0) && Input.GetKey(KeyCode.LeftShift))
        {
            _selectorPrefab.StopDraw();
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if(selectedControllableUnits.Count <= 0) return;
            
            foreach (var unit in selectedControllableUnits)
            {
                unit.SelectComponent.IsCurrentPlayer = false;
            }
            
            currentUnitNumber = (currentUnitNumber+1) % selectedControllableUnits.Count;
            selectedControllableUnits[currentUnitNumber].SelectComponent.IsCurrentPlayer = true;
        }
    }

    public void AddControl(Character character)
    {
        _contoller = character;
        Debug.Log(_contoller.name);
    }

    private bool CanControl(Character character)
    {
        if (character is MinionComponent component && component.HeroParent.GetComponent<HeroComponent>() == _contoller)
        {
            return true;
        }

        if (character == _contoller)
        {
            return true;
        }

        return false;
    }

    public void SelectOnClick(Character character)
    {
        DeselectAll();
        selectedControllableUnits.Add(character);
        character.SelectComponent.IsSelect = true;
    }

    public void SelectInArea(Character character)
    {
        if (!CanControl(character))
        {
            Debug.Log("cant control");
            return;
        }
        
        if (!selectedControllableUnits.Contains(character))
        {
            selectedControllableUnits.Add(character);
            character.SelectComponent.NumberInGroup = selectedControllableUnits.IndexOf(character);
            character.SelectComponent.IsSelect = true;
        }
        else
        {
            selectedControllableUnits.Remove(character);
            character.SelectComponent.IsSelect = false;
        }

        selectedControllableUnits.FirstOrDefault()!.SelectComponent.IsCurrentPlayer = true;
        currentUnitNumber = 0;
    }

    public void Deselect(Character character)
    {
        if(selectedControllableUnits.Contains(character)) 
            selectedControllableUnits.Remove(character);
    }
    public void DeselectAll()
    {
        foreach (var character in selectedControllableUnits)
        {
            character.SelectComponent.IsSelect = false;
        }
        selectedControllableUnits.Clear();
    }
}
