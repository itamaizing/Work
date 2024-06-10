using System;
using System.Collections;
using UnityEngine;

public class Spisnacider : MonoBehaviour
{
    [SerializeField] private GameObject AcidPrefab;
    [SerializeField] private Transform Target;
    [HideInInspector] public AttackRangeType AttackRangeType;
    [HideInInspector] public GameObject Player;

    private CharacterData _playerData;
    private HealthComponent _healthComponent;
    private float _timer = 0f;
    private float _interval = 1f;

    private float _evasionMeleeAttack = 0.15f;
    //private float _evasionRangeAttack = 0.05f;

    private GameObject _newAcidPrefab;
    private float _distance = 1.94f * 4;
    private CarrygunControlledObjects _controlledObjectsScript;
    private Coroutine _attackCoroutine;
    private bool _canActivateAbilities = true;
    private GameObject _target;
    private SelectObject _select;
    private float healthOriginal;
    private float speedOriginal;



    void Start()
    {
        _healthComponent.OnTakePhisicDamage += DamageMeleeEvasion;
        _healthComponent.OnTakeMagicDamage += DamageMeleeEvasion;

        Player.transform.Find("Talents").gameObject.GetComponent<ContagionOfEmbrio>().controlledObjects.Add(gameObject);
        _controlledObjectsScript = Player.transform.Find("Abilities").gameObject.GetComponent<CarrygunControlledObjects>();

        _select = FindObjectOfType<SelectObject>();
        healthOriginal = _playerData.Health;
        speedOriginal = GetComponent<CharacterData>().MoveSpeed;
    }

    void Update()
    {
        CheckTarget();

        if (!CheckForSlime(transform.position))
        {
            ReduceHealth();
            GetComponent<MoveComponent>().SetDefaultSpeed();
        }
        else
        {
            GetComponent<MoveComponent>().SetMoveSpeed(0.2f);
        }

        AttackRangeType = AttackRangeType.RangeAttack;

        _target = _controlledObjectsScript.TargetObject;
    }

    bool CheckForSlime(Vector2 point)
    {
        Collider2D collider = Physics2D.OverlapCircle(point, 0.1f);
        if (collider != null)
        {
            slime slime = collider.gameObject.GetComponent<slime>();
            return slime != null;
        }

        return false;
    }

    private void CheckTarget()
    {
        if (_controlledObjectsScript.NewPrefabPoint != null)
        {
            Target.position = _controlledObjectsScript.NewPrefabPoint.transform.position;
        }
        else if (_controlledObjectsScript.TargetObject != null && _controlledObjectsScript.TargetObject != gameObject)
        {
            Target.position = _controlledObjectsScript.TargetObject.transform.position;
            AttackTarget(_controlledObjectsScript.TargetObject);
        }
    }

    private void ReduceHealth()
    {
        _timer += Time.deltaTime;

        if (_timer >= _interval)
        {
            _healthComponent.TryTakeDamage(2, DamageType.Physical, AttackRangeType.MeleeAttack);
            _timer = 0f;
        }
    }

    private void DamageMeleeEvasion(HealthComponent.DamageInfo damageInfo)
    {
        Type callerType = damageInfo.CallerType;


        damageInfo.ModifiedDamage -= damageInfo.ModifiedDamage * _evasionMeleeAttack;
    }

    public void AttackTarget(GameObject target)
    {
        if (_canActivateAbilities)
        {
            float distanceToTarget = (Target.position - transform.position).magnitude;

            if (distanceToTarget <= _distance && _attackCoroutine == null)
            {
                _attackCoroutine = StartCoroutine(Attack(target));
            }
        }
    }

    private IEnumerator Attack(GameObject target)
    {
        float distanceToTarget = (Target.position - transform.position).magnitude;

        if (distanceToTarget <= _distance)
        {
            int damage = UnityEngine.Random.Range(8, 11);
            float damageSpeed = 1.8f;

            yield return new WaitForSeconds(damageSpeed * 0.75f);

            if (_newAcidPrefab == null)
            {
                _newAcidPrefab = Instantiate(AcidPrefab, transform.position, Quaternion.identity);
                _newAcidPrefab.GetComponent<Acid>().Target = _target;
                _newAcidPrefab.GetComponent<Acid>().Damage = damage;
                _newAcidPrefab.GetComponent<Acid>().Spisnscider = gameObject;
            }

            _canActivateAbilities = false;

            yield return new WaitForSeconds(damageSpeed * 0.25f);

            _canActivateAbilities = true;
            _attackCoroutine = null;
        }

        _newAcidPrefab = null;
    }

    private void OnDestroy()
    {
        if (Player != null)
        {
            Player.transform.Find("Talents").gameObject.GetComponent<ContagionOfEmbrio>().controlledObjects.Remove(gameObject);
        }
    }
}
