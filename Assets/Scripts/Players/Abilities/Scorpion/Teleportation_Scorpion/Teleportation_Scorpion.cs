using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Teleportation_Scorpion : Ability
{
    [Header("Ability settings")]
    [SerializeField] private DrawCircle _drawCircleSelf;
    [SerializeField] private float _minRadius;
    [SerializeField] private int _baseManaCost;
    [SerializeField] private int _manaCostPerTile = 5;
    [SerializeField] private LayerMask _layerMask;
    private DrawCircle _circleTarget;
    private MoveComponent _target;
    private Coroutine _useJob;

    protected override void Cancel()
    {
        if (_useJob != null)
            StopCoroutine(_useJob);

        ResetValue();

        if (_circleTarget != null)
            Destroy(_circleTarget.gameObject);
    }

    protected override void Cast()
    {
        _useJob = StartCoroutine(UseCoroutine());
    }

    private void ResetValue()
    {
        IsCanCancle = true;
        _drawCircleSelf.Clear();
        _target = null;
    }

    private bool IsMouseInRadius()
    {
        float distance = Vector3.Distance(
            new Vector3(Camera.main.ScreenToWorldPoint(Input.mousePosition).x, Camera.main.ScreenToWorldPoint(Input.mousePosition).y, transform.position.z),
            transform.position
            );

        return distance <= GetCurrentRadius() /*Radius*/;
    }

    private float GetCurrentRadius()
    {
        return Radius + 0.2f * CalculateCurrentScale();
    }
    private int CalculateCurrentScale()
    {
        if(_mana.Value >= 20)
        {
            return (int)((_mana.Value - 20) / 1);
        }

        return 0;
    }

    private IEnumerator UseCoroutine()
    {
        Debug.LogWarning(CalculateCurrentScale());
        _drawCircleSelf.Draw(GetCurrentRadius());
        Debug.LogWarning(GetCurrentRadius());

        while (_target == null) //выбираем цель
        {
            if (Input.GetMouseButtonDown(0) && IsMouseInRadius())
            {
                RaycastHit2D[] rayHit = Physics2D.RaycastAll(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
                if (rayHit.Length > 0 && rayHit[0].transform.TryGetComponent<MoveComponent>(out MoveComponent enemyMove))
                {
                    _target = enemyMove;
                }
            }
            yield return null;
        }
        _drawCircleSelf.Clear();

        IsCanCancle = false;
        yield return GetCastDeleyCoroutine();
        PayCost();

        Vector3 directionToEnemy = _target.transform.position - transform.position;
        directionToEnemy.Normalize();
        Vector3 offset = directionToEnemy * 2f;
        Vector3 teleportPosition = _target.transform.position + offset;
        bool touchObstacle = Physics2D.OverlapCircle(teleportPosition, 2f, _layerMask);

        if(touchObstacle) // если попадем в препятствие
        {
            bool plaseFound = false;
            float angle = 0f;
            Vector3 newPosition;

            while (angle != 180 && !plaseFound) // ищем ближайшее место без препятствий
            {
                angle += 10;
                newPosition = _target.transform.position + Quaternion.Euler(0,0,angle) * offset;
                if (!Physics2D.OverlapCircle(newPosition, 1f, _layerMask))
                {
                    Debug.Log($"Найдено место чтобы не попасть в препятствие, поворот на {angle} градусов");
                    plaseFound=true;
                    teleportPosition = newPosition;
                }
                angle *= -1;
                newPosition = _target.transform.position + Quaternion.Euler(0, 0, angle) * offset;
                if (!Physics2D.OverlapCircle(newPosition, 1f, _layerMask))
                {
                    Debug.Log($"Найдено место чтобы не попасть в препятствие, поворот на {angle} градусов");
                    plaseFound = true;
                    teleportPosition = newPosition;
                }
                angle *= -1;

            }
        }
        Debug.LogWarning(directionToEnemy);

        PlayerMove.transform.position = teleportPosition; // сам тп

        IsCanCancle = true;
        //PayCost();

        ResetValue();
    }
}
