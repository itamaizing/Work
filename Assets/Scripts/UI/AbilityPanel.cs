using System;
using System.Collections.Generic;
using UnityEngine;

public class AbilityPanel : MonoBehaviour
{
    [SerializeField] private AbilityIcon _abilityIconPref;
    [SerializeField] private FillAmountOverTime _castLine;
    
    private PlayerAbilities _playerAbilities;
    private List<Ability> _abilities;
    private List<AbilityIcon> _abilityIcons;
    private bool _isActive;
    public bool _isSelect;

    public bool IsSelect
    {
        get => _isSelect;
        set => _isSelect = value;
    }

    public bool IsActive
    {
        get => _isActive;
        set
        {
            _isActive = value && IsSelect;
            gameObject.SetActive(_isActive);
        }
    }
    
    private void Awake()
    {
        _abilities = new List<Ability>();
        _abilityIcons = new List<AbilityIcon>();
        IsActive = false;
    }

    public void Fill(PlayerAbilities abilities)
    {
        _playerAbilities = abilities;
        _abilities.AddRange(_playerAbilities.Abilities); 
        
        foreach (var item in _abilities)
        {
            AbilityIcon abilityIcon = Instantiate(_abilityIconPref, transform);
            abilityIcon.Init(item, _castLine);
            _abilityIcons.Add(abilityIcon);
        }

        _playerAbilities.AbilitySelected += OnAbilitySelected;
        _playerAbilities.AbilityDeselected += OnAbilityDeselected;
        _playerAbilities.AbilityAutoAttackSelected += OnAutoAttackAbilitySelected;
        _playerAbilities.AbilityAutoAttackDeselected += OnAutoAttackAbilityDeselected;
    }

    public void DestroyAbilityPanel()
    {
        Destroy(gameObject);
    }

    private void OnAbilitySelected(int index)
    {
        _abilityIcons[index].Selected();
    }

    private void OnAbilityDeselected(int index)
    {
        _abilityIcons[index].Deselected();
    }

    private void OnAutoAttackAbilitySelected(int index)
    {
        _abilityIcons[index].AutoAttackSelected();
    }

    private void OnAutoAttackAbilityDeselected(int index)
    {
        _abilityIcons[index].AutoAttackDeselected();
    }
}
