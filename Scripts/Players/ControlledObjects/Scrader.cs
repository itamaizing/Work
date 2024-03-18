using Pathfinding;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Scrader : MonoBehaviour
{
    [SerializeField] private GameObject BleedEffectPrefab;
    [SerializeField] private GameObject AbilityPanel;
    [SerializeField] private Toggle AbilityToggleEvolution;
    [SerializeField] private GameObject IconAbility;
    [SerializeField] private GameObject CastPrefab;
    [SerializeField] private GameObject CocoonPrefab;
    [SerializeField] private Transform Target;

    [HideInInspector] public AttackRangeType AttackRangeType;
    [HideInInspector] public GameObject Player;

    private HealthPlayer _healthPlayer;
    private float _timer = 0f;
    private float _interval = 1f;

    private bool _canActivateAbilities = true;
    private bool _finalCast;

    private float _evasionPhisicAttack = 0.05f;
    private float _attackSpeed = 0.7f;
    private float _chanceBleeding = 0.15f;

    private GameObject _newBleedPrefab;
    private GameObject _newCastPrefab;
    private GameObject _newCocoonPrefab;

    private Coroutine _attackCoroutine;

    private SelectObject _select;

    private CarrygunControlledObjects _controlledObjectsScript;

    private float healthOriginal;
    private float speedOriginal;


    void Start()
    {
        _healthPlayer = GetComponent<HealthPlayer>();
        _healthPlayer.OnTakePhisicDamage += DamageEvasion;

        _healthPlayer.OnTakePhisicDamage += CheckTakeAttack;
        _healthPlayer.OnTakeMagicDamage += CheckTakeAttack;

        Transform canvasAbilities = GameObject.Find("CanvasAbilities").transform;

        AbilityPanel.SetActive(true);
        AbilityPanel.transform.parent = canvasAbilities;

        AttackRangeType = AttackRangeType.MeleeAttack;

        Player.transform.Find("Talents").gameObject.GetComponent<ContagionOfEmbrio>().controlledObjects.Add(gameObject);
        _controlledObjectsScript = Player.transform.Find("Abilities").gameObject.GetComponent<CarrygunControlledObjects>();

        _select = FindObjectOfType<SelectObject>();

        healthOriginal = _healthPlayer.MaxHealth;
        speedOriginal = GetComponent<PlayerMove>().MoveSpeed;

    }

    void Update()
    {
        CheckScradersObject();

        if (!CheckForSlime(transform.position))
        {
            ReduceHealth();
            _healthPlayer.MaxHealth = healthOriginal;
            GetComponent<PlayerMove>().MoveSpeed = speedOriginal;
        }
        else
        {
            _healthPlayer.MaxHealth += healthOriginal * 0.1f;
            GetComponent<PlayerMove>().MoveSpeed += speedOriginal * 0.1f;
        }

        CheckTarget();

        if (AbilityToggleEvolution.isOn)
        {
            IconAbility.GetComponent<SpriteRenderer>().enabled = true;
            Color newColor = IconAbility.GetComponent<SpriteRenderer>().color;
            newColor.a = 1f;
            IconAbility.GetComponent<SpriteRenderer>().color = newColor;

            if (!_canActivateAbilities) 
                AbilityToggleEvolution.isOn = false;
            else if (_canActivateAbilities)
                CreateCastPrefab(2);
        }
        else if (!AbilityToggleEvolution.isOn)
        {
            IconAbility.GetComponent<SpriteRenderer>().enabled = false;
        }

        if (_healthPlayer.Health <= 0)
        {
            Destroy(gameObject);
        }

        if(AbilityPanel != null)
        {
            AbilityPanel.GetComponent<RectTransform>().anchoredPosition = new Vector3(0, 35, 0);
            AbilityPanel.GetComponent<RectTransform>().localScale = Vector2.one;
        }

        if (_finalCast)
        {
            Evolution();
        }
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
        else if(_controlledObjectsScript.TargetObject != null && _controlledObjectsScript.TargetObject != gameObject)
        {
            Target.position = _controlledObjectsScript.TargetObject.transform.position;
            AttackTarget(_controlledObjectsScript.TargetObject);
        }
    }

    private void Evolution()
    {

        if (_newCocoonPrefab == null)
        {
            _newCocoonPrefab = Instantiate(CocoonPrefab, gameObject.transform.position, Quaternion.identity);
            _newCocoonPrefab.GetComponent<Cocoon>().Player = Player;
        }

        _select.ControlledObjects.Remove(gameObject);
        Destroy(gameObject);
    }

    private void CheckTakeAttack(HealthPlayer.DamageInfo damageInfo)
    {
        string thisType = "Scrader";

        if (AbilityToggleEvolution.isOn && Convert.ToString(damageInfo.CallerType) != thisType)
        {
            AbilityToggleEvolution.isOn = false;

            if (_newCastPrefab != null)
            {
                Destroy(_newCastPrefab);
            }
        }
    }

    private void CheckScradersObject()
    {
        float distance = 1.94f * 0.7f * 2;
        Scrader[] scraderComponents = FindObjectsOfType<Scrader>();

        foreach (Scrader scrader in scraderComponents)
        {
            if (scrader.gameObject != gameObject)
            {
                if((scrader.transform.position - gameObject.transform.position).magnitude <= distance)
                {
                    _attackSpeed -= _attackSpeed * 0.1f;
                    break;
                }
                else
                {
                    _attackSpeed = 0.7f;
                }
            }
        }
    }

    private void ReduceHealth()
    {
        _timer += Time.deltaTime;

        if (_timer >= _interval)
        {
            _healthPlayer.TakePhisicDamage(2);
            _timer = 0f;
        }
    }

    private void DamageEvasion(HealthPlayer.DamageInfo damageInfo)
    {
        damageInfo.ModifiedDamage -= damageInfo.ModifiedDamage * _evasionPhisicAttack;
    }

    public void AttackTarget(GameObject target)
    {
        if (_canActivateAbilities)
        {
            float objectToTarget = (target.transform.position - gameObject.transform.position).magnitude;
            float distance = 1.94f * 0.7f;

            if (objectToTarget <= distance && _attackCoroutine == null)
            {
                _attackCoroutine = StartCoroutine(Attack(target));
            }
        }
    }

    private IEnumerator Attack(GameObject target)
    {
        int damage = UnityEngine.Random.Range(1, 4);
        yield return new WaitForSeconds(_attackSpeed / 2);

        target.GetComponent<HealthPlayer>().TakePhisicDamage(damage);

        if(UnityEngine.Random.value <= _chanceBleeding)
        {
            if (_newBleedPrefab == null)
            {
                _newBleedPrefab = Instantiate(BleedEffectPrefab);
                _newBleedPrefab.transform.SetParent(target.transform);
                _newBleedPrefab.GetComponentInChildren<BaffDebaffEffectPrefab>().StartCountdown(3);
            }
            else
            {
                target.transform.Find("Effects").transform.Find("ScraderBleedEffect").GetComponentInChildren<BaffDebaffEffectPrefab>().Timer = 3;
                _newBleedPrefab = Instantiate(BleedEffectPrefab);

                foreach (Transform child in _newBleedPrefab.transform)
                {
                    Destroy(child.gameObject);
                }

                _newBleedPrefab.transform.SetParent(target.transform);
            }
        }
        _canActivateAbilities = false;

        yield return new WaitForSeconds(_attackSpeed / 2);

        _canActivateAbilities = true;
        _attackCoroutine = null;
    }

    public void CreateCastPrefab(float time)
    {
        Vector2 newVector = new Vector2(gameObject.transform.position.x, gameObject.transform.position.y - 1);
        _newCastPrefab = Instantiate(CastPrefab, newVector, Quaternion.identity);
        _newCastPrefab.transform.SetParent(gameObject.transform);
        Transform childObject = _newCastPrefab.transform.Find("GameObject");
        if (childObject != null)
        {
            StartCoroutine(ScaleObjectOverTime(childObject, 1f, time));
        }
    }

    private IEnumerator ScaleObjectOverTime(Transform targetTransform, float targetScaleX, float duration)
    {
        float elapsedTime = 0f;
        Vector3 initialScale = targetTransform.localScale;
        Vector3 targetScale = new Vector3(targetScaleX, targetTransform.localScale.y, targetTransform.localScale.z);

        while (elapsedTime < duration && targetTransform != null && AbilityToggleEvolution.isOn)
        {
            targetTransform.localScale = Vector3.MoveTowards(initialScale, targetScale, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        if (targetTransform != null)
        {
            targetTransform.localScale = targetScale;
        }
        _finalCast = true;
       Destroy(_newCastPrefab);
    }

    private void OnDestroy()
    {
        Destroy(AbilityPanel);

        Player.transform.Find("Talents").gameObject.GetComponent<ContagionOfEmbrio>().controlledObjects.Remove(gameObject);
    }
}
