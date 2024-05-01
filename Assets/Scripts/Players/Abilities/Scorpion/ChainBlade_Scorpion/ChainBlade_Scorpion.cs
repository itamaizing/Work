using Players.Abilities.Genjalf.Fireworks_Ability;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ChainBlade_Scorpion : Ability
{
    [Header("Ability settings")]
    [SerializeField] private DrawCircle _drawCircleSelf;
    [SerializeField] private float _range;

    [SerializeField] private BladeProjectile _bladePrefab;
    private BladeProjectile _blade;

    private DrawCircle _circleTarget;
    private PlayerMove _target;
    private Coroutine _useJob;

    private GameObject enemy;

    private bool isAlternativeCast = false;

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
        _drawCircleSelf.Clear();
        _target = null;
    }

    private bool IsMouseInRadius()
    {
        float distance = Vector3.Distance(
            new Vector3(Camera.main.ScreenToWorldPoint(Input.mousePosition).x, Camera.main.ScreenToWorldPoint(Input.mousePosition).y, transform.position.z),
            transform.position
            );

        return distance <= Radius;
    }

    private IEnumerator PullEnemy()
    {
        PayCost();
        //yield return GetCastDeleyCoroutine();

        //enemy.transform.position = transform.position;
        float distance = Vector2.Distance(transform.position, enemy.transform.position);

        while (distance >= 2f)
        {
            enemy.transform.position = Vector2.MoveTowards(enemy.transform.position, transform.position, 5f * Time.deltaTime);
            distance = Vector2.Distance(transform.position, enemy.transform.position);
            yield return null;

        }
    }
    private IEnumerator UseCoroutine()
    {
        if (!isAlternativeCast)
        {
            _drawCircleSelf.Draw(Radius);
            bool isCliked = false;

            while (isCliked == false) //выбираем цель
            {
                if (Input.GetMouseButtonDown(0) && IsMouseInRadius())
                {
                    isCliked = true;
                }
                yield return null;
            }
            _drawCircleSelf.Clear();

            IsCanCancle = false;
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            yield return GetCastDeleyCoroutine();

            _blade = Instantiate(_bladePrefab, transform.position, Quaternion.identity);
            _blade.Init(8f);
            _blade.ThrowBlade(mousePosition - transform.position);
            _blade.OnHit.AddListener(target => { enemy = target; if (target != null) isAlternativeCast = true; /*if(target != null)StartCoroutine(PullEnemy());*/ }); // подписка на метод, получаем цель в которую попули. Отписка автоматическая при уничтожении префаба будет

            IsCanCancle = true;
            PayCost();

            ResetValue();
        }


        //альтернативный каст

        if (isAlternativeCast)
        {
            //PayCost();
            //yield return GetCastDeleyCoroutine();

            //enemy.transform.position = transform.position;
            float distance = Vector2.Distance(transform.position, enemy.transform.position);

            while (distance >= 2f)
            {
                enemy.transform.position = Vector2.MoveTowards(enemy.transform.position, transform.position, 5f * Time.deltaTime);
                distance = Vector2.Distance(transform.position, enemy.transform.position);
                yield return null;

            }
            isAlternativeCast = false;

            PayCost();
        }

    }
}
