using UnityEngine;

public class AuraTriggerForwarder : MonoBehaviour
{
    [SerializeField] private ReconnaissanceFireAura _aura;

    private void OnTriggerEnter(Collider other)
    {
        _aura?.HandleTriggerEnter(other);
    }

    private void OnTriggerExit(Collider other)
    {
        _aura?.HandleTriggerExit(other);
    }
}
