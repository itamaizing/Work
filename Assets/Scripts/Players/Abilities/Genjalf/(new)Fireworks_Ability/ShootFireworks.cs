using System.Collections;
using System.Collections.Generic;
using GlobalEvents;
using UnityEngine;
using UnityEngine.UI;

public class ShootFireworks : Ability
{
    [Header("Ability settings")]
    [SerializeField] Fireworks _fireworksPref;
    [SerializeField] private float _manaCostPerTick;
    [SerializeField] private float _duration;
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

    private List<HealthPlayer> _enemies = new List<HealthPlayer>();

    private Fireworks _fireworks;
    private Coroutine _useJob;

    private void OnValidate()
    {
        if (_positionForExtraWidth > _length)
            _positionForExtraWidth = _length;
    }

    public override void Use()
    {
        if (IsReady)
        {
            IsReady = false;
            _useJob = StartCoroutine(UseCoroutine());
        }
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

    private IEnumerator UseCoroutine()
    {
        _fireworks = Instantiate(_fireworksPref, transform);
        _fireworks.SetLength(_length);
        _fireworks.SetWidth(_width);
        _fireworks.SetPositionForExtraWidth(_positionForExtraWidth * 2);
        _fireworks.SetExtraWidth(_extraWidth);

        while (Input.GetMouseButtonDown(0) == false)
        {
            _fireworks.RotateAtMouse();
            yield return null;
        }
        _fireworks.Activate();

        RaycastHit2D[] rayHit = Physics2D.RaycastAll(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
        if (rayHit.Length > 0 && rayHit[0].transform.CompareTag("Enemies"))
        {
            _fireworks.SetTarget(rayHit[0].transform);
        }
        float time = 0;
        float damageTime = 0;

        while (time < _duration)
        {
            time += Time.deltaTime;
            damageTime += Time.deltaTime;

            Mana.UseMana(_manaCostPerTick);

            SortEnemiesByDistance();

            if (damageTime < _damageRate)
            {
                yield return null;
                continue;
            }

            _enemies.Clear();

            foreach (var item in _fireworks.Collisions)
            {
                if (item.TryGetComponent<HealthPlayer>(out HealthPlayer enemy) && item.transform != transform.parent)
                {
                    _enemies.Add(enemy);
                }
            }

            for (int i = 0; i < _enemies.Count; i++)
            {
                float currentDamage = Random.Range(_minDamagePerTick, _maxDamagePerTick + 1);
                switch (i)
                {
                    case 0:
                        _enemies[i].TakeMagicDamage(currentDamage * _percentFirstTarget);
                        break;
                    case 1:
                        _enemies[i].TakeMagicDamage(currentDamage * _percentSecondTarget);
                        break;
                    case 2:
                        _enemies[i].TakeMagicDamage(currentDamage * _percentThirdTarget);
                        break;
                    default:
                        _enemies[i].TakeMagicDamage(currentDamage * _percentOtherTarget);
                        break;
                }
            }
            damageTime = 0;
            yield return null;
        }
        IsReady = true;
        Destroy(_fireworks.gameObject);
    }
}