using UnityEngine;

[CreateAssetMenu(fileName = "DamageAbility", menuName = "Ability/Damage")]
public class DamageAbilityData : AbilityData
{
    [SerializeField] private float _damage;
    [SerializeField] private float _spellSpeed;
    [SerializeField] private bool _isPeriodic;

    public override float MainValue => _damage;
    public override AbilityDataType AbilityType => AbilityDataType.Damage;
    public float Speed => _spellSpeed;
    public bool IsPeriodic => _isPeriodic;
    
}