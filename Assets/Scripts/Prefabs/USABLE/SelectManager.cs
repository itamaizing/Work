using System;
using System.Collections.Generic;
using UnityEngine;

public class SelectManager : MonoBehaviour
{
    [SerializeField] private BoxSelector _selectorPrefab;
    
    [SerializeField] private LayerMask allies;
    [SerializeField] private LayerMask enemies;

    private static SelectManager instance; 
    public static SelectManager Instance => instance;

    private List<Character> controllableUnits;
    public List<Character> selectedControllableUnits;

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

        controllableUnits = new List<Character>();
        selectedControllableUnits = new List<Character>();
        _selectorPrefab.gameObject.SetActive(false);
    }
    
    private void LateUpdate()
    {
        if (Input.GetMouseButtonDown(0)&&Input.GetKey(KeyCode.LeftShift))
        {
            _selectorPrefab.gameObject.SetActive(true);
            _selectorPrefab.StartDraw();
        }

        if (Input.GetMouseButton(0))
        {
            _selectorPrefab.Draw();
        }

        if (Input.GetMouseButtonUp(0))
        {
            _selectorPrefab.StopDraw();
        }
    }

    public void SelectOnClick(Character character)
    {
        DeselectAll();
        selectedControllableUnits.Add(character);
        character.UIPlayerComponents.ChangeSelection(true);
    }

    public void SelectInArea(Character character)
    {
        if (!selectedControllableUnits.Contains(character))
        {
            selectedControllableUnits.Add(character);
            character.UIPlayerComponents.ChangeSelection(true);
        }
        else
        {
            selectedControllableUnits.Remove(character);
            character.UIPlayerComponents.ChangeSelection(false);
        }
    }
    public void DeselectAll()
    {
        foreach (var character in selectedControllableUnits)
        {
            character.UIPlayerComponents.ChangeSelection(false);
        }
        selectedControllableUnits.Clear();
    }
}
