using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fisura : Ability
{
    [Header("Ability settings")]
    [SerializeField] private float _radius;
    [SerializeField] private DrawCircle _drawCircle;
    [SerializeField] private FisuraTail _fisuraTilePrefab;
    [SerializeField] private float _width;
    [SerializeField] private float _length;
    [SerializeField] private float _liveTime;

    private FisuraTail _fisuraTile;
    private Coroutine _useJob;

    public override void Use()
    {
        if (IsReady)
        {
            IsReady = false;
            _drawCircle.Draw(_radius);
            _useJob = StartCoroutine(UseCoroutine());
        }
    }

    private bool IsMouseInRadius()
    {
        float distance = Vector3.Distance(
            new Vector3(Camera.main.ScreenToWorldPoint(Input.mousePosition).x, Camera.main.ScreenToWorldPoint(Input.mousePosition).y, transform.position.z),
            transform.position
            );

        return distance <= _radius;
    }

    private IEnumerator UseCoroutine()
    {
        Vector2 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        _fisuraTile = Instantiate(_fisuraTilePrefab, worldPosition, Quaternion.identity, null);

        while (Input.GetMouseButtonDown(0) == false)
        {
            if (IsMouseInRadius())
            {
                _fisuraTile.transform.position = new Vector3(Camera.main.ScreenToWorldPoint(Input.mousePosition).x, Camera.main.ScreenToWorldPoint(Input.mousePosition).y, 0);
            }
            yield return null;
        }
        yield return null;

        _fisuraTile.SetSize(new Vector2(_width, 0));

        Vector3 _spawnPoint = _fisuraTile.transform.position;
        Vector3 targetPosition;
        float distance;
        float lastDistance = 0;
        float deltaDistance;

        while (Input.GetMouseButtonDown(0) == false)
        {
            if (IsMouseInRadius())
            {
                _fisuraTile.Rotate();

                targetPosition = new Vector3(Camera.main.ScreenToWorldPoint(Input.mousePosition).x, Camera.main.ScreenToWorldPoint(Input.mousePosition).y, 0);
                distance = Vector3.Distance(_spawnPoint, targetPosition);

                if (distance <= _length * 2)
                {
                    deltaDistance = distance - lastDistance;
                    lastDistance = distance;

                    _fisuraTile.AddLength(deltaDistance / 2);
                }
            }
            yield return null;
        }
        _fisuraTile.Activate(_liveTime);
        _drawCircle.Clear();
        IsReady = true;
    }
}
