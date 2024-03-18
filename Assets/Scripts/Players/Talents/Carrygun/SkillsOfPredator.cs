using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillsOfPredator : MonoBehaviour
{
    [SerializeField] private GameObject MarkOfPredator;
    [SerializeField] private Toggle _toggleTalent;

    private ThreeMeleeAttack _jumpAbility;
    private OneMeleeAttack _handDamageAbility;
    public List<string> eventOrder = new List<string>();
    private GameObject _target;
    private GameObject _newTarget;
    private GameObject _newMarkPrefab;
    private GameObject _player;
    private GameObject _playerAbility;

    private Dictionary<GameObject, GameObject> markedObjects = new Dictionary<GameObject, GameObject>();

    public event System.Action<MarkOfPredator> ScriptInstanceDestroyed;
    private int maxScriptInstances = 2;
    [HideInInspector] public int ScriptInstanceCount = 0;

    private void Start()
    {
        _player = transform.parent.gameObject;
        _playerAbility = _player.transform.Find("Abilities").gameObject;

        // Получение ссылок на экземпляры
        _jumpAbility = _playerAbility.GetComponent<ThreeMeleeAttack>();
        _handDamageAbility = _playerAbility.GetComponent<OneMeleeAttack>();

        // Подписка на события использования способностей
        _jumpAbility.ThirdAbilityEvent += damage => OnAbilityUsed("Ability1");
        _handDamageAbility.FirstAbilityEvent += damage => OnAbilityUsed("Ability2");

        _playerAbility.GetComponent<TwoMeleeAttack>().SecondAbilityEvent += damage => Reset();
        _playerAbility.GetComponent<FourMeleeAttack>().FourthAbilityEvent += damage => Reset();
    }

    private void OnAbilityUsed(string abilityName)
    {
        if (_toggleTalent.isOn)
        {
            if (abilityName == "Ability1")
            {
                if (_target == null)
                {
                    _target = _jumpAbility.TargetParent;
                }

                _newTarget = _jumpAbility.TargetParent;
            }
            else if (abilityName == "Ability2")
            {
                if (_target == null)
                {
                    _target = _handDamageAbility.TargetParent;
                }

                _newTarget = _handDamageAbility.TargetParent;
            }

            if (_target == _newTarget)
            {
                eventOrder.Add(abilityName);
                CheckEventOrderCount();

                // Проверка наличия комбо
                if (CheckCombo())
                {
                    Debug.Log("Combo!");
                    AddMark(_target);
                }
            }
            else
            {
                Reset();
            }
        }
    }

    private bool CheckCombo()
    {
        // Проверка наличия комбо
        if (eventOrder.Count >= 3 &&
            eventOrder[eventOrder.Count - 1] == "Ability1" &&
            eventOrder[eventOrder.Count - 2] == "Ability2" &&
            eventOrder[eventOrder.Count - 3] == "Ability1")
        {
            Debug.Log("Combo");
            eventOrder.Clear();
            return true;
        }
        else if(eventOrder.Count >= 3)
        {
            eventOrder.Clear();
        }
        return false;
    }

    private void CheckEventOrderCount()
    {
        if (eventOrder.Count == 1)
        {
            if (Random.value < 0.2f)
            {
                _playerAbility.GetComponent<Bleeding>().StartBleed(_target, _playerAbility.GetComponent<ThreeMeleeAttack>());
            }
        }
        else if (eventOrder.Count == 2)
        {
            if (_handDamageAbility.DamageRate == 0)
            {
                if (Random.value < 0.9f)
                {
                    _playerAbility.GetComponent<Bleeding>().StartBleed(_target, _playerAbility.GetComponent<OneMeleeAttack>());
                }
            }
            else
            {
                if (Random.value < 0.4f)
                {
                    _playerAbility.GetComponent<Bleeding>().StartBleed(_target, _playerAbility.GetComponent<OneMeleeAttack>());
                }
            }
        }
    }

    private void AddMark(GameObject target)
    {

        if (markedObjects.ContainsKey(target) && ScriptInstanceCount < maxScriptInstances ||
            markedObjects.Count == 0 && ScriptInstanceCount < maxScriptInstances)
        {
            // Если метка уже есть, добавляем вторую метку
            _newMarkPrefab = Instantiate(MarkOfPredator);
            _newMarkPrefab.GetComponent<MarkOfPredator>().Player = transform.parent.gameObject;
            MarkOfPredator newScript = _newMarkPrefab.GetComponent<MarkOfPredator>();
            newScript.DestroyedMark += damage => OnScriptInstanceDestroyed(target);

            _newMarkPrefab.transform.SetParent(target.transform);
            markedObjects[target] = _newMarkPrefab;

            ScriptInstanceCount++;
        }
        else
        {
            return;
        }
    }

    private void OnScriptInstanceDestroyed(GameObject target)
    {
        ScriptInstanceDestroyed?.Invoke(markedObjects[target].GetComponent<MarkOfPredator>());
        markedObjects.Remove(target);
        ScriptInstanceCount--;
    }


    private void Reset()
    {
        eventOrder.Clear();
        _target = null;
        _newTarget = null;
    }

    private void OnDisable()
    {

        _jumpAbility.ThirdAbilityEvent -= damage => OnAbilityUsed("Ability1");
        _handDamageAbility.FirstAbilityEvent -= damage => OnAbilityUsed("Ability2");

        if (_player != null)
        {
            _playerAbility.GetComponent<TwoMeleeAttack>().SecondAbilityEvent -= damage => Reset();
            _playerAbility.GetComponent<FourMeleeAttack>().FourthAbilityEvent -= damage => Reset();
        }
    }
}
