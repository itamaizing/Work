using UnityEngine;

[CreateAssetMenu(fileName = "MovementAbility", menuName = "Ability/Movement")]
public class MovementAbilityData : AbilityData
{
    [SerializeField] private float _movementDistance;
    [SerializeField]  private float _movementSpeed;

    public override float MainValue => _movementDistance;
    
    public override AbilityDataType AbilityType => AbilityDataType.Movement;

    public float Speed => _movementSpeed;
}