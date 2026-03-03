using UnityEngine;

public class CreatureCarryGun : MonoBehaviour
{
    private float _speedModifier = 1;
    public float SpeedModifier { get => _speedModifier; set => _speedModifier = value; }
}
