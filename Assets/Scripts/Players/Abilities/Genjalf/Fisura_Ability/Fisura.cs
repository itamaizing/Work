using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fisura : Ability
{
    [Header("Ability settings")]
    [SerializeField] private FisuraTail _fisuraTilePrefab;
    [SerializeField] private float _width;
    [SerializeField] private float _length;
    [SerializeField] private float _angelTileLength;
    [SerializeField] private float _liveTime;

    private FisuraTail _fisuraTile;
    private FisuraTail _fisuraTileRight;
    private FisuraTail _fisuraTileLeft;
    private Coroutine _useJob;

    protected override void Cast()
    {
        _useJob = StartCoroutine(UseCoroutine());
    }

    protected override void Cancel()
    {
        StopCoroutine(_useJob);
        Destroy(_fisuraTile.gameObject);
    }

    private bool IsMouseInRadius()
    {
        float distance = Vector3.Distance(
            new Vector3(Camera.main.ScreenToWorldPoint(Input.mousePosition).x, Camera.main.ScreenToWorldPoint(Input.mousePosition).y, transform.position.z),
            transform.position
            );

        return distance <= Radius;
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

    private IEnumerator UseCoroutine()
    {
        Vector2 mouseStartPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        _fisuraTile = Instantiate(_fisuraTilePrefab, mouseStartPosition, Quaternion.identity, null);
        _fisuraTile.SetSize(new Vector2(0, 0));

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
            yield return GetCastDeleyCoroutine();
            _fisuraTile.Rotate(PlayerMove.MoveDirection);
            _fisuraTile.transform.Translate(Vector2.right * 2);
            _fisuraTile.SetSizeWithoutOffset(new Vector2(_width, _length));
            AddAngleTileWithoutOffset();
            FisuraActivate();

            PayCost();
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
            //if (IsMouseInRadius()) //код был нужен, чтобы нельзя было сделать фисуру дальше радиуса
            //{
                _fisuraTile.Rotate();

                targetPosition = new Vector3(Camera.main.ScreenToWorldPoint(Input.mousePosition).x, Camera.main.ScreenToWorldPoint(Input.mousePosition).y, 0);
                distance = Vector3.Distance(_spawnPoint, targetPosition);

                if (distance <= _length * 2)
                {
                    deltaDistance = distance - lastDistance;
                    lastDistance = distance;

                    _fisuraTile.AddLength(deltaDistance / 2);
                }
            //}
            yield return null;
        }
        yield return GetCastDeleyCoroutine();
        AddAngleTile();
        FisuraActivate();

        PayCost();
    }
}
