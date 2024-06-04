using System.Collections.Generic;
using UnityEngine;

public class AbilityPanel : MonoBehaviour
{
    [SerializeField] private AbilityIcon _abilityIconPref;
    [SerializeField] private FillAmountOverTime _castLine;
    [SerializeField] private PlayerAbilities _playerAbilities;

    private List<Ability> _abilities = new List<Ability>();
    private List<AbilityIcon> _abilityIcons = new List<AbilityIcon>();

    private void Start()
    {
        List<Ability> abilities = _playerAbilities.Abilities;

        foreach (var item in abilities)
        {
            _abilities.Add(item);
        }
        UpdateAbilityList();

        _playerAbilities.AbilitySelected += OnAbilitySelected;
        _playerAbilities.AbilityDeselected += OnAbilityDeselected;
    }

    public void UpdateAbilityList(List<Ability> abilities)
    {
        _abilities = abilities;

        foreach (var item in _abilities)
        {
            AbilityIcon abilityIcon = Instantiate(_abilityIconPref, transform);
            abilityIcon.Init(item, _castLine);
            _abilityIcons.Add(abilityIcon);
        }
    }

    private void UpdateAbilityList()
    {
        foreach (var item in _abilities)
        {
            AbilityIcon abilityIcon = Instantiate(_abilityIconPref, transform);
            abilityIcon.Init(item, _castLine);
            _abilityIcons.Add(abilityIcon);
        }
    }

    private void OnAbilitySelected(int index)
    {
        _abilityIcons[index].Selected();
    }

    private void OnAbilityDeselected(int index)
    {
        _abilityIcons[index].Deselected();
    }
}
