using DG.Tweening;
using UnityEngine;
using System;

public class ArrowFireProjectile : MonoBehaviour
{
    [SerializeField] private float _flightDuration = 1f;
    [SerializeField] private float _baseSpeed = 10f;
    [SerializeField] private float _minDuration = 0.5f;
    [SerializeField] private float _maxDuration = 2.5f;

    #region Const
    private const float _midPointDuration = 0.5f;
    private const float _halfDuration = 2f;
    private const int BezierApproxSteps = 10;
    #endregion

    private float _arcHeight;

    public event Action<Vector3> OnProjectilePathEnd;

    public void Init(Vector3 targetPoint, float arcHeight)
    {
        _arcHeight = arcHeight;
        Launch(targetPoint);
    }


    public void Launch(Vector3 targetPoint)
    {
        Vector3 startPoint = transform.position;
        float distance = Vector3.Distance(startPoint, targetPoint);
        float midHeight = Mathf.Max(startPoint.y, targetPoint.y) + _arcHeight;
        Sequence seq = DOTween.Sequence();

        Vector3 midPoint = Vector3.Lerp(startPoint, targetPoint, _midPointDuration);
        midPoint.y = midHeight;

        float estimatedArcLength = ApproximateQuadraticBezierLength(startPoint, midPoint, targetPoint);
        float duration = Mathf.Clamp(estimatedArcLength / _baseSpeed, _minDuration, _maxDuration);

        seq.Append(transform.DOMove(midPoint, duration / _halfDuration).SetEase(Ease.OutQuad));
        seq.Append(transform.DOMove(targetPoint, duration / _halfDuration).SetEase(Ease.InQuad));

        seq.OnComplete(() =>
        {
            OnProjectilePathEnd?.Invoke(targetPoint);
            Destroy(gameObject);
        });
    }

    private float ApproximateQuadraticBezierLength(Vector3 p0, Vector3 p1, Vector3 p2, int steps = BezierApproxSteps)
    {
        float length = 0f;
        Vector3 prevPoint = p0;

        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector3 point = Mathf.Pow(1 - t, 2) * p0 + 2 * (1 - t) * t * p1 + Mathf.Pow(t, 2) * p2;
            length += Vector3.Distance(prevPoint, point);
            prevPoint = point;
        }

        return length;
    }
}
