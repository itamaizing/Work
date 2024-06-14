using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Ability/AbilityInfo", fileName = "AbilityInfo")]
public class AbilityInfo : ScriptableObject
{
    [SerializeField] private string _name;
    [SerializeField] private string _description;
    [SerializeField] private Sprite _icon;

    public string Name => _name;
    public string Description => _description;
    public Sprite Icon => _icon;
}
