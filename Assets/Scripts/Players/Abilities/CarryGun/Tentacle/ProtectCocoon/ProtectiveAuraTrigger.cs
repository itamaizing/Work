using UnityEngine;

public class ProtectiveAuraTrigger : MonoBehaviour
{
    [SerializeField] private ProtectiiveCocoonAuraDamage _protectiiveCocoonAuraDamage;

    private void OnTriggerEnter(Collider other)
    {
        _protectiiveCocoonAuraDamage?.HandleTriggerEnter(other);
        Debug.Log("Противник в радиусе");
    }

    private void OnTriggerExit(Collider other)
    {
        _protectiiveCocoonAuraDamage?.HandleTriggerExit(other);
    }
}
