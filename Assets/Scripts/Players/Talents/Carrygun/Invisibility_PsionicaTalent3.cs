using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Invisibility_PsionicaTalent3 : MonoBehaviour
{
    [SerializeField] private Toggle _toggleTalent;
    [SerializeField] private Toggle _toggleAbility;
    [SerializeField] private GameObject _cooldownButton;
    [SerializeField] private GameObject _abilityPrefab;
    [SerializeField] private GameObject _iconAbility;
    [SerializeField] private GameObject _iconNoBlink;
    [SerializeField] private DrawCircle _drawCircle;
    [SerializeField] private GameObject _markEffect;
    [SerializeField] private GameObject _invisibleEffect;
    [SerializeField] private GameObject CastPrefab;

    [HideInInspector] public bool IsInvisibility;

    private GameObject _player;
    private GameObject _newEffectPrefab;
    private GameObject _newCastPrefab;
    private float _abilityCooldownTime;
    private Coroutine _castCoroutine;
    private Coroutine _stopCoroutine;

    void Start()
    {
        _player = transform.parent.gameObject;

        _player.GetComponent<HealthComponent>().OnTakeMagicDamage += TakeDamage;
        _player.GetComponent<HealthComponent>().OnTakePhisicDamage += TakeDamage;

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

        if (_toggleAbility.isOn && _toggleAbility.gameObject.activeSelf)
        {
            HandleToggleAbilityOn();
        }
        else if (_toggleAbility.isOn == false)
        {
            HandleToggleAbilityOff();
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

        if (Input.GetKeyDown((KeyCode)Enum.Parse(typeof(KeyCode), key)) && _toggleAbility.gameObject.activeSelf && _toggleAbility.transform.parent.gameObject.activeSelf)
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
    }

    private void HandleToggleAbilityOn()
    {
        if(_castCoroutine == null)
        {
            _castCoroutine = StartCoroutine(CastCoroutine());
        }

        _iconAbility.GetComponent<SpriteRenderer>().enabled = true;
        Color newColor = _iconAbility.GetComponent<SpriteRenderer>().color;
        newColor.a = 1f;
        _iconAbility.GetComponent<SpriteRenderer>().color = newColor;

    }

    private void HandleToggleAbilityOff()
    {
        IsInvisibility = false;

        if(_stopCoroutine != null)
        {
            StopCoroutine(_stopCoroutine);
            _stopCoroutine = null;
            DisactivateInvisibility();
        }

        _iconAbility.GetComponent<SpriteRenderer>().enabled = false;
        _castCoroutine = null;
    }

    private IEnumerator CastCoroutine()
    {
        _player.GetComponent<MoveComponent>().CanMove = false;

        CreateCastPrefab(2.7f);

        yield return new WaitForSeconds(2.7f);

        _player.GetComponent<MoveComponent>().CanMove = true;

        ActivateInvisibility();
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

    private void ActivateInvisibility()
    {
        IsInvisibility = true;

        //_player.GetComponent<CharacterState>().AddState(new InvisibleState(),0,0,0);//TODO ADDVALUES

        _newEffectPrefab = Instantiate(_invisibleEffect);
        _newEffectPrefab.transform.SetParent(_player.transform);
        _newEffectPrefab.GetComponentInChildren<BaffDebaffEffectPrefab>().StartCountdown(12);

        for (int i = 0; i < _newEffectPrefab.transform.childCount; i++)
        {
            _newEffectPrefab.transform.GetChild(i).SetParent(_player.transform.Find("Effects"));
        }

        _stopCoroutine = StartCoroutine(Stop());
    }

    private void DisactivateInvisibility()
    {
        IsInvisibility = false;

        if (_player.GetComponent<CharacterState>().IfHasState(new InvisibleState()))
        {
           // _player.GetComponent<CharacterState>().AddState(new DefaultState(),0,0,0);//TODO ADDVALUES
        }

        StartCoroutine(RechargeCoroutine());
    }


    private void TakeDamage(HealthComponent.DamageInfo damageInfo)
    {
        if(_toggleAbility.isOn)
        {
            DisactivateInvisibility();
        }
    }

    private IEnumerator Stop()
    {
        yield return new WaitForSeconds(12f);

        DisactivateInvisibility();
    }

    private IEnumerator RechargeCoroutine()
    {
        _abilityCooldownTime = 11;

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
}
