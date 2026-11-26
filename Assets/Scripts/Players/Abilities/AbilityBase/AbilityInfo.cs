using UnityEngine;

[CreateAssetMenu(menuName = "Ability/AbilityInfo", fileName = "AbilityInfo")]
public class AbilityInfo : ScriptableObject
{
    [SerializeField] private string _name;
    [SerializeField] private string _counter;
    [SerializeField] private Sprite _icon;
    [SerializeField] private GameDescriptions.DescriptionTable _descriptionTable = new();

    public string Name => _name;
    public string Counter => _counter;
    public Sprite Icon => _icon;
    public GameDescriptions.DescriptionTable DescriptionTable => _descriptionTable;
}
