using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fisura : Ability
{
    [Header("Ability settings")]
    [SerializeField] private PlayerMove _playerMove;
    [SerializeField] private float _radius;
    [SerializeField] private DrawCircle _drawCircle;
    [SerializeField] private FisuraTail _fisuraTilePrefab;
    [SerializeField] private float _width;
    [SerializeField] private float _length;
    [SerializeField] private float _angelTileLength;
    [SerializeField] private float _liveTime;
    [SerializeField] private float _castDeley;

    private FisuraTail _fisuraTile;
    private FisuraTail _fisuraTileRight;
    private FisuraTail _fisuraTileLeft;
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

    private void AddAngleTile()
    {
        _fisuraTileLeft = Instantiate(_fisuraTilePrefab, _fisuraTile.transform.position, _fisuraTile.transform.rotation, _fisuraTile.transform);
        _fisuraTileLeft.SetSize(new Vector2(_width, _angelTileLength));
        _fisuraTileLeft.transform.Translate(new Vector3(_fisuraTile.Size.x * 2, 0, 0));

        _fisuraTileRight = Instantiate(_fisuraTilePrefab, _fisuraTile.transform.position, _fisuraTile.transform.rotation, _fisuraTile.transform);
        _fisuraTileRight.SetSize(new Vector2(_width, _angelTileLength));
        _fisuraTileRight.transform.Translate(new Vector3(_fisuraTile.Size.x * 2, _fisuraTile.Size.y * 2 - _fisuraTileRight.Size.y * 2, 0));
    }

    private void AddAngleTileWithoutOffset()
    {
        _fisuraTileLeft = Instantiate(_fisuraTilePrefab, _fisuraTile.transform.position, _fisuraTile.transform.rotation, _fisuraTile.transform);
        _fisuraTileLeft.SetSize(new Vector2(_width, _angelTileLength));
        _fisuraTileLeft.transform.Translate(new Vector3(-_fisuraTile.Size.x * 2, -_fisuraTile.Size.y, 0));

        _fisuraTileRight = Instantiate(_fisuraTilePrefab, _fisuraTile.transform.position, _fisuraTile.transform.rotation, _fisuraTile.transform);
        _fisuraTileRight.SetSize(new Vector2(_width, _angelTileLength));
        _fisuraTileRight.transform.Translate(new Vector3(-_fisuraTile.Size.x * 2, _fisuraTile.Size.y - _fisuraTileRight.Size.y * 2, 0));
    }

    private void FisuraActivate()
    {
        _fisuraTile.Activate(_liveTime);
        if(_fisuraTileRight != null && _fisuraTileLeft != null)
        {
            _fisuraTileRight.Activate(_liveTime);
            _fisuraTileLeft.Activate(_liveTime);
        }
    }

    private IEnumerator CastDeley()
    {
        float time = 0;
        while(time < _castDeley)
        {
            time += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator UseCoroutine()
    {
        Vector2 mouseStartPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        _fisuraTile = Instantiate(_fisuraTilePrefab, mouseStartPosition, Quaternion.identity, null);

        while (Input.GetMouseButtonDown(0) == false)
        {
            if (IsMouseInRadius())
            {
                _fisuraTile.transform.position = new Vector3(Camera.main.ScreenToWorldPoint(Input.mousePosition).x, Camera.main.ScreenToWorldPoint(Input.mousePosition).y, 0);
            }
            yield return null;
        }
        yield return null;

        RaycastHit2D[] rayHit = Physics2D.RaycastAll(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
        if (rayHit.Length > 0 && rayHit[0].transform == transform.parent)
        {
            yield return StartCoroutine(CastDeley());
            _fisuraTile.Rotate(_playerMove.DirectionOfMovement);
            _fisuraTile.transform.Translate(Vector2.right * 2);
            _fisuraTile.SetSizeWithoutOffset(new Vector2(_width, _length));
            AddAngleTileWithoutOffset();
            FisuraActivate();

            _drawCircle.Clear();
            IsReady = true;
            yield break;
        }

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
        yield return StartCoroutine(CastDeley());
        AddAngleTile();
        FisuraActivate();

        _drawCircle.Clear();
        IsReady = true;
    }
}
