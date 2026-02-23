using UnityEngine;

public class LightningAttack : MonoBehaviour
{
    [SerializeField] private float lifeTime = 1f;

    public void Init(Vector3 startPos, Transform target)
    {
        transform.position = startPos;

        if (target != null)
        {
            Vector3 targetPos = target.position + Vector3.up * 1f;
            Vector3 direction = (targetPos - startPos).normalized;

            transform.right = direction;
        }

        Destroy(gameObject, lifeTime);
    }
}