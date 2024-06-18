using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PsionicCharge_PsionicaTalent1 : MonoBehaviour
{
    [SerializeField] private Toggle _toggleTalent;
    [SerializeField] private Toggle _toggleAbility;
    [SerializeField] private GameObject _cooldownButton;
    [SerializeField] private GameObject CastPrefab;

    private float _abilityCooldownTime;
    private float _timerCharge;
    private float _timerBlinding;
    private GameObject _player;
    private GameObject _target;
    private GameObject _newCastPrefab;
    private Coroutine _castCoroutine;

    void Start()
    {
        _player = transform.parent.gameObject;

        _player.transform.Find("Abilities").gameObject.GetComponent<OneMeleeAttack>().FirstAbilityEvent += damage => CheckClawsStrike();
        _player.transform.Find("Abilities").gameObject.GetComponent<FiveConversion>().UseActivePsionicaEvent += CheckUsedActivePsionica;
    }

    void Update()
    {
        if (_toggleTalent.isOn && _toggleAbility.gameObject.activeSelf == false)
        {
            _toggleAbility.gameObject.SetActive(true);
        }
        else if (!_toggleTalent.isOn && _toggleAbility.gameObject.activeSelf)
        {
            _toggleAbility.gameObject.SetActive(false);
        }

        int activeChildCount = 0;
        foreach (Transform child in _toggleAbility.transform.parent)
        {
            if (child.gameObject.activeSelf)
            {
                activeChildCount++;

                if (child.gameObject == _toggleAbility.gameObject)
                {
                    break;
                }
            }
        }
        string key = "Alpha" + activeChildCount;

        if (activeChildCount < 10 && Input.GetKeyDown((KeyCode)Enum.Parse(typeof(KeyCode), key)) && _toggleAbility.gameObject.activeSelf && _toggleAbility.transform.parent.gameObject.activeSelf)
        {
            if (_cooldownButton.gameObject.activeSelf)
            {
                _cooldownButton.GetComponent<Button>().onClick.Invoke();
            }
            else
            {
                if (_toggleAbility.enabled)
                {
                    _toggleAbility.isOn = !_toggleAbility.isOn;
                }
            }
        }

        if (_toggleAbility.isOn && _castCoroutine == null)
        {
            if (_player.GetComponent<PsionicaMelee>().Psionica >= 10)
            {
                _player.GetComponent<PsionicaMelee>().UsePsionica(10f);
                _castCoroutine = StartCoroutine(CastCoroutine());
            }
            else
            {
                _toggleAbility.isOn = false;
            }
        }

        //������ �������� ����� �������
        if (_timerCharge > 0)
        {
            _timerCharge -= Time.deltaTime;
        }

        if (_timerCharge <= 0 && _toggleAbility.isOn)
        {
            RechargeCoroutine();
        }

        //���� ���������
        if (_timerBlinding > 0)
        {
            _timerBlinding -= Time.deltaTime;

            if (_timerBlinding <= 0)
            {
                //_target.GetComponent<CharacterState>().AddState(new DefaultState(),0,0,0);//TODO ADDVALUES
                _target = null;
            }
        }
    }

    private IEnumerator CastCoroutine()
    {
        _player.GetComponent<MoveComponent>().CanMove = false;

        CreateCastPrefab(2.2f);
        yield return new WaitForSeconds(2.2f);

        _player.GetComponent<MoveComponent>().CanMove = true;
        _timerCharge = 6f;

    }

    public void CreateCastPrefab(float time)
    {
        Vector2 newVector = new Vector2(_player.transform.position.x, _player.transform.position.y - 1);
        _newCastPrefab = Instantiate(CastPrefab, newVector, Quaternion.identity);
        _newCastPrefab.transform.SetParent(_player.transform);
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

        while (elapsedTime < duration && targetTransform != null)
        {
            targetTransform.localScale = Vector3.MoveTowards(initialScale, targetScale, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        if (targetTransform != null)
        {
            targetTransform.localScale = targetScale;
        }
    }

    //������� ���������� ��� ����� �������
    private void CheckClawsStrike()
    {
        if (_toggleAbility.isOn)
        {
            _target = _player.transform.Find("Abilities").gameObject.GetComponent<OneMeleeAttack>().Target;
            //_target.GetComponent<CharacterState>().AddState(new BlindnessState(),0,0,0);//TODO ADDVALUES

            float psionica = _player.GetComponent<PsionicaMelee>().Psionica;

            if (psionica > 60)
            {
                _player.GetComponent<PsionicaMelee>().UsePsionica(60);
                _timerBlinding = 6f;
            }
            else
            {
                _player.GetComponent<PsionicaMelee>().UsePsionica(psionica);
                _timerBlinding = psionica * 0.1f;
            }

            _toggleAbility.isOn = false;
            _timerCharge = 0;
        }
    }

    private void CheckUsedActivePsionica(float value, GameObject target)
    {
        if(_toggleAbility.isOn)
        {
            if (_target != null && _target.GetComponent<Mana>())
            {
                float mana = _target.GetComponent<Mana>().Value;

                if(mana < value)
                {
                    _target.GetComponent<Mana>().Use(mana);
                    _player.GetComponent<PsionicaMelee>().MakePsionica(mana);
                }
                else
                {
                    _target.GetComponent<Mana>().Use(value);
                    _player.GetComponent<PsionicaMelee>().MakePsionica(value);
                }
            }
        }
    }

    private IEnumerator RechargeCoroutine()
    {
        _abilityCooldownTime = 12;
        _timerCharge = 0;

        _toggleAbility.isOn = false;
        _toggleAbility.enabled = false;
        _cooldownButton.gameObject.SetActive(true);

        while (_abilityCooldownTime > 0)
        {
            _cooldownButton.GetComponentInChildren<TextMeshPro>().text = ((int)_abilityCooldownTime + 1).ToString();
            _abilityCooldownTime -= Time.deltaTime;
            yield return null;
        }

        _cooldownButton.gameObject.SetActive(false);
        _toggleAbility.enabled = true;
    }

    private void OnDisable()
    {
        _player.transform.Find("Abilities").gameObject.GetComponent<OneMeleeAttack>().FirstAbilityEvent -= damage => CheckClawsStrike();
        _player.transform.Find("Abilities").gameObject.GetComponent<FiveConversion>().UseActivePsionicaEvent -= CheckUsedActivePsionica;
    }
}
