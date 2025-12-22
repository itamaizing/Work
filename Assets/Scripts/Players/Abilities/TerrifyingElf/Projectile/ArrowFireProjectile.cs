using DG.Tweening;
using UnityEngine;
using System;

public class ArrowFireProjectile : MonoBehaviour
{
    [SerializeField] private float _flightDuration = 1f;

    #region Const
    private const float _midPointDuration = 0.5f;
    #endregion
    private float _arcHeight;

    public static event Action<Vector3> OnProjectilePathEnd;

    public void Init(Vector3 targetPoint, float arcHeight)
    {
        _arcHeight = arcHeight;
        Launch(targetPoint);
    }


    public void Launch(Vector3 targetPoint)
    {
        Vector3 startPoint = transform.position;
        float midHeight = Mathf.Max(startPoint.y, targetPoint.y) + _arcHeight;
        Sequence seq = DOTween.Sequence();

        Vector3 midPoint = Vector3.Lerp(startPoint, targetPoint, _midPointDuration);
        midPoint.y = midHeight;

        seq.Append(transform.DOMove(midPoint, _flightDuration).SetEase(Ease.OutQuad));
        seq.Append(transform.DOMove(targetPoint, _flightDuration).SetEase(Ease.InQuad));

        seq.OnComplete(() =>
        {
            OnProjectilePathEnd?.Invoke(targetPoint);
            Destroy(gameObject);
        });
    }
}
