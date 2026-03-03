using UnityEngine;

public class CreatureCarryGun : MonoBehaviour
{
    [SerializeField] private float _speedModifier;

    public float SpeedModifier { get => _speedModifier; set => _speedModifier = value; }
}
