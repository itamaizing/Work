using UnityEngine;

public class JumpPassable : MonoBehaviour
{
    [SerializeField] private Collider targetCollider;
    [SerializeField] private LayerMask passableLayer;

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & passableLayer) != 0)
        {
            if (targetCollider != null) targetCollider.enabled = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & passableLayer) != 0)
        {
            if (targetCollider != null) targetCollider.enabled = true;
        }
    }
}
