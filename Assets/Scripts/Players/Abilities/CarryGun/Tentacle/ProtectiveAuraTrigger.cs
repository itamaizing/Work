using UnityEngine;

public class ProtectiveAuraTrigger : MonoBehaviour
{
    [SerializeField] private ProtectiiveCocoonAuraDamage _protectiiveCocoonAuraDamage;
    private void OnTriggerEnter(Collider other)
    {
        _protectiiveCocoonAuraDamage?.HandleTriggerEnter(other);
    }

    private void OnTriggerExit(Collider other)
    {
        _protectiiveCocoonAuraDamage?.HandleTriggerExit(other);
    }
}
