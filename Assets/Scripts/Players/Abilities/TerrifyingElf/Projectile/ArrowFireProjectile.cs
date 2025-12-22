using DG.Tweening;
using UnityEngine;
using System;

public class ArrowFireProjectile : MonoBehaviour
{
    [SerializeField] private float _flightDuration = 1f;

    private ReconnaissanceFire _skillOwner;
    private float _arcHeight;

    public static event Action<Vector3> OnProjectilePathEnd;

    public void Init(Vector3 targetPoint, ReconnaissanceFire owner, float arcHeight)
    {
        _arcHeight = arcHeight;
        _skillOwner = owner;
        Launch(targetPoint);
    }


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
            if (_skillOwner != null) _skillOwner.NotifyProjectileEnded(targetPoint);
            Destroy(gameObject);
        });
    }
}
