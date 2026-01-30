using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Ability/AbilityInfo", fileName = "AbilityInfo")]
public class AbilityInfo : ScriptableObject
{
    [SerializeField] private string _name;
    [SerializeField] private string _description;
    [SerializeField] private string _state;
    [SerializeField] private string _descriptionState;
    [SerializeField] private string _counter;
    [SerializeField] private Sprite _icon;

    private string _addingDescription;

    public string AddingDescription { get => _addingDescription; set => _addingDescription = value; }
    public string Name => _name;
    public string Description => _description;
    public string State => _state;
    public string DescriptionState => _descriptionState;
    public string Counter => _counter;
    public Sprite Icon => _icon;

    private void OnEnable()
    {
        _addingDescription = _description;
    }

    public void AddingDescriptionSet(bool value, string text)
    {
        _addingDescription = value ? _description + $" {text}" : _description;
    }
}
