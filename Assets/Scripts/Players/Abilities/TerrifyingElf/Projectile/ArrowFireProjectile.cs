using DG.Tweening;
using UnityEngine;
using System;

public class ArrowFireProjectile : MonoBehaviour
{
    [SerializeField] private float _flightDuration = 1f;
    [SerializeField] private float _arcHeight = 2f;

    public static event Action<Vector3> OnProjectilePathEnd;

    public void Launch(Vector3 targetPoint)
    {
        Vector3 startPoint = transform.position;
        float midHeight = Mathf.Max(startPoint.y, targetPoint.y) + _arcHeight;
        Sequence seq = DOTween.Sequence();

        Vector3 midPoint = Vector3.Lerp(startPoint, targetPoint, 0.5f);
        midPoint.y = midHeight;

        seq.Append(transform.DOMove(midPoint, _flightDuration / 2f).SetEase(Ease.OutQuad));

        seq.Append(transform.DOMove(targetPoint, _flightDuration / 2f).SetEase(Ease.InQuad));

        seq.OnComplete(() =>
        {
            OnProjectilePathEnd?.Invoke(targetPoint);
            Destroy(gameObject);
        });
    }
}
