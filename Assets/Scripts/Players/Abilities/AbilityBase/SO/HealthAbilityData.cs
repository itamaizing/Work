using UnityEngine;

[CreateAssetMenu(fileName = "HealthAbility", menuName = "Ability/Heal")]
public class HealthAbilityData : AbilityData
{
    [SerializeField] private float _heal;
    [SerializeField]  private float _spellSpeed;
    [SerializeField]  private bool _isPeriodic;
    
    public override float MainValue => _heal;
    public override AbilityDataType AbilityType => AbilityDataType.Heal;
    public float Speed => _spellSpeed;
    public bool IsPeriodic => _isPeriodic;
}