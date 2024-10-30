using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinionPanel : MonoBehaviour
{
    [SerializeField] private SelectManager _selectManager;
    [SerializeField] private MinionIcon _minionIconPref;

    private List<MinionIcon> _minionIcons = new List<MinionIcon>();
    private Character _hero;

    private void Start()
    {
        if (_selectManager != null)
        {
            _selectManager.CharacterSelected += OnCharacterSelected;
            _selectManager.CharacterDeselected += OnCharacterDeselected;
        }
    }

    private void OnDestroy()
    {
        if (_selectManager != null)
        {
            _selectManager.CharacterSelected -= OnCharacterSelected;
            _selectManager.CharacterDeselected -= OnCharacterDeselected;
        }
    }

    private void OnCharacterSelected(Character character)
    {
        foreach (var item in character.SpawnComponent.Units)
        {
            var temp = Instantiate(_minionIconPref, transform);
            temp.Init(item);
            _minionIcons.Add(temp);
        }
        _hero = character;
        _hero.SpawnComponent.UnitAdded += OnUnitAdded;
        _hero.SpawnComponent.UnitRemoved += OnUnitRemoved;
    }

    private void OnCharacterDeselected(Character character)
    {
        if(_hero != null)
        {
            _hero.SpawnComponent.UnitAdded -= OnUnitAdded;
            _hero.SpawnComponent.UnitRemoved -= OnUnitRemoved;
            _hero = null;
        }

        foreach (var item in _minionIcons)
        {
            Destroy(item.gameObject);
        }
        _minionIcons.Clear();
    }

    private void OnUnitAdded(MinionComponent minion)
    {
        UpdatePanel();
    }
    
    private void OnUnitRemoved()
    {
        UpdatePanel();
    }

    private void UpdatePanel()
    {
        var temp = _hero;
        OnCharacterDeselected(null);
        OnCharacterSelected(temp);
    }
}
