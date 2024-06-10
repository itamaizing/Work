using System.Collections;
using System.Collections.Generic;
using GlobalEvents;
using UnityEngine;
using UnityEngine.UI;

public class ShootFireworks : Ability
{
    [Header("Ability settings")]
    [SerializeField] private Fireworks _fireworksPref;
    [Header("Size")]
    [SerializeField] private float _length;
    [SerializeField] private float _width;
    [SerializeField] private float _extraWidth;
    [SerializeField] private float _positionForExtraWidth;
    [Header("Damage")]
    [SerializeField] private float _damageRate;
    [SerializeField] private float _minDamagePerTick;
    [SerializeField] private float _maxDamagePerTick;
    [SerializeField] private float _percentFirstTarget;
    [SerializeField] private float _percentSecondTarget;
    [SerializeField] private float _percentThirdTarget;
    [SerializeField] private float _percentOtherTarget;

    private List<HealthComponent> _enemies = new List<HealthComponent>();

    private Fireworks _fireworks;
    private Coroutine _useJob;

    private void OnValidate()
    {
        if (_positionForExtraWidth > _length)
            _positionForExtraWidth = _length;
    }

    protected override void Cast()
    {
         _useJob = StartCoroutine(UseCoroutine());
    }

    protected override void Cancel()
    {
        StopCoroutine(_useJob);

        if(_fireworks != null)
            Destroy(_fireworks.gameObject);

        PlayerMove.CanMove = true;
    }

    private void SortEnemiesByDistance()
    {
        _fireworks.Collisions.Sort(CompareDistanceToMe);
    }

    private int CompareDistanceToMe(Collider2D a, Collider2D b)
    {
        float squaredRangeA = (a.transform.position - transform.position).sqrMagnitude;
        float squaredRangeB = (b.transform.position - transform.position).sqrMagnitude;
        return squaredRangeA.CompareTo(squaredRangeB);
    }

    private void CreateFireworks()
    {
        _fireworks = Instantiate(_fireworksPref, transform);
        _fireworks.SetLength(_length);
        _fireworks.SetWidth(_width);
        _fireworks.SetPositionForExtraWidth(_positionForExtraWidth * 2);
        _fireworks.SetExtraWidth(_extraWidth);
    }

    private void DoDamage()
    {
        for (int i = 0; i < _enemies.Count; i++)
        {
            float currentDamage = Random.Range(_minDamagePerTick, _maxDamagePerTick + 1);
            switch (i)
            {
                case 0:
                    _enemies[i].TryTakeDamage(currentDamage * _percentFirstTarget, DamageType.Magical, AttackRangeType.RangeAttack);
                    break;
                case 1:
                    _enemies[i].TryTakeDamage(currentDamage * _percentFirstTarget, DamageType.Magical, AttackRangeType.RangeAttack);
                    break;
                case 2:
                    _enemies[i].TryTakeDamage(currentDamage * _percentFirstTarget, DamageType.Magical, AttackRangeType.RangeAttack);
                    break;
                default:
                    _enemies[i].TryTakeDamage(currentDamage * _percentFirstTarget, DamageType.Magical, AttackRangeType.RangeAttack);
                    break;
            }
        }
    }

    private IEnumerator UseCoroutine()
    {
        CreateFireworks();

        while (Input.GetMouseButtonDown(0) == false)
        {
            _fireworks.RotateAtMouse();
            yield return null;
        }
        _fireworks.Activate();
        PlayerMove.CanMove = false;

        RaycastHit2D[] rayHit = Physics2D.RaycastAll(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
        if (rayHit.Length > 0 && rayHit[0].transform.CompareTag("Enemies"))
            _fireworks.RotateAtTarget(rayHit[0].transform);

        float time = 0 + _damageRate * 2;
        float damageTime = 0;

        IsCanCancle = false;
        PayCost();

        while (time < StreamingDuration)
        {
            time += Time.deltaTime;
            damageTime += Time.deltaTime;

            if (damageTime < _damageRate)
            {
                yield return null;
                continue;
            }
            _enemies.Clear();

            foreach (var item in _fireworks.Collisions)
            {
                if (item.TryGetComponent<HealthComponent>(out HealthComponent enemy) && item.transform != transform.parent)
                {
                    _enemies.Add(enemy);
                }
            }
            SortEnemiesByDistance();
            DoDamage();
            damageTime = 0;
            yield return null;
        }
        PlayerMove.CanMove = true;
        Destroy(_fireworks.gameObject);
    }
}