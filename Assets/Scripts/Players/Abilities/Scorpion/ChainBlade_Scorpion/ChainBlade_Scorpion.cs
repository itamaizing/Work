using Players.Abilities.Genjalf.Fireworks_Ability;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ChainBlade_Scorpion : Ability
{
    [Header("Ability settings")]
    [SerializeField] private CastLine _castlinePrefab;
    private CastLine _castLine;

    [SerializeField] private DrawCircle _drawCircleSelf;
    [SerializeField] private float _range;
    [SerializeField] private ChainController _chainPrefab;
    private ChainController _chain;

    [SerializeField] private BladeProjectile _bladePrefab;
    private BladeProjectile _blade;

    private DrawCircle _circleTarget;
    private MoveComponent _target;
    private Coroutine _useJob;

    private GameObject enemy;
    private bool bladeDestroyed = false;
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
        bladeDestroyed = false;
        _drawCircleSelf.Clear();
        _target = null;
        if(_castLine != null)
        {
            Destroy(_castLine.gameObject);
        }
    }

    private bool IsMouseInRadius()
    {
        float distance = Vector3.Distance(
            new Vector3(Camera.main.ScreenToWorldPoint(Input.mousePosition).x, Camera.main.ScreenToWorldPoint(Input.mousePosition).y, transform.position.z),
            transform.position
            );

        return distance <= Radius;
    }

    private IEnumerator UseCoroutine()
    {
        
            _castLine = Instantiate(_castlinePrefab, transform);
            _drawCircleSelf.Draw(Radius);
            bool isCliked = false;

            while (isCliked == false) //выбираем цель
            {
                if (Input.GetMouseButtonDown(0) && IsMouseInRadius())
                {
                    isCliked = true;
                }
                _castLine.RotateAtMouse();
                yield return null;
            }
            Vector2 castLineEndPoint = _castLine.targetPoint.transform.position - transform.position;
            Destroy(_castLine.gameObject);
            _drawCircleSelf.Clear();

            IsCanCancle = false;
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            //yield return GetCastDeleyCoroutine();

            _blade = Instantiate(_bladePrefab, transform.position, Quaternion.identity);
            _chain = Instantiate(_chainPrefab);
            _chain.AssignTarget(transform ,_blade.transform);
            _blade.Init(8f);
            _blade.ThrowBlade(/*mousePosition - transform.position*/ castLineEndPoint);
            _blade.OnHit.AddListener(target => 
            { enemy = target; 
                if (target != null) isAlternativeCast = true;
                if (target == null) Destroy(_chain.gameObject); 
                else _chain.AssignTarget(transform, enemy.transform);
                bladeDestroyed = true;
            }); // подписка на метод, получаем цель в которую попали. Отписка автоматическая при уничтожении префаба будет
            IsCanCancle = true;
            while (!bladeDestroyed)
            {
                yield return null;
            }

            Debug.LogWarning("Закончилось");

            IsCanCancle = true;
            //PayCost(); // не могу дважды потратить ману, тк после этого можно опять юзать абилку и багуется

        //альтернативный каст

        if (isAlternativeCast)
        {
            IsCanCancle = false;
            PlayerMove.CanMove = false;

            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            enemy.GetComponent<MoveComponent>().CanMove = false;

            while (distance >= 2f)
            {
                enemy.transform.position = Vector2.MoveTowards(enemy.transform.position, transform.position, 10f * Time.deltaTime);
                distance = Vector2.Distance(transform.position, enemy.transform.position);
                yield return null;

            }
            enemy.GetComponent<MoveComponent>().CanMove = true;
            Destroy(_chain.gameObject);
            isAlternativeCast = false;
            PlayerMove.CanMove = true;
            //PayCost();
        }
        PayCost();
        ResetValue();

    }
}
