using UnityEngine;

public class LightningAttack : MonoBehaviour
{
    [SerializeField] private float lifeTime = 1f;

    public void Init(Vector3 startPos, Transform target)
    {
        transform.position = startPos;

        if (target != null)
        {
            Vector3 direction = (target.position - startPos).normalized;
            transform.rotation = Quaternion.LookRotation(direction);
        }

        Destroy(gameObject, lifeTime);
    }
}