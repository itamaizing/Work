using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AbilityBanDatabase", menuName = "Databases/AbilityBanDatabase")]
public class AbilityBanDatabase : ScriptableObject
{
    public List<string> abilityNames = new();
}
