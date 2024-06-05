using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Copy_PsionicaTalent1 : MonoBehaviour
{
    [SerializeField] private Toggle _toggleTalentCopy;
    [SerializeField] private Toggle _toggleTalentInvis;
    [SerializeField] private Toggle _toggleAbility;
    [SerializeField] private GameObject _cooldownButton;
    [SerializeField] private GameObject _abilityPrefab;
    [SerializeField] private GameObject _iconAbility;
    [SerializeField] private GameObject _iconNoBlink;
    [SerializeField] private DrawCircle _drawCircle;
    [SerializeField] private GameObject _bleedEffect;
    [SerializeField] private GameObject _markEffect;


    private GameObject _player;
    private GameObject _copyTarget;
    private GameObject _target;
    private GameObject _newAbilityPrefab;
    private GameObject _newMarkEffectPrefab;
    private GameObject _iconCoursor;
    private float _abilityCooldownTime;
    private float _distance;
    private Vector2 _abilityPosition;
    private bool _isPrefab;
    private bool _cursorIsActive;
    private bool _canDrawCircle;
    private bool _bleedCopied;
    private bool _markCopied;
    private Coroutine _blinkCoroutine;

    void Start()
    {
        _player = transform.parent.gameObject;
        _distance = 5 * 1.94f;
    }

    // Update is called once per frame
    void Update()
    {
        IncreasingPsionicsCreated();

        if ((_toggleTalentCopy.isOn || _toggleTalentInvis.isOn) && _toggleAbility.gameObject.activeSelf == false)
        {
            _toggleAbility.gameObject.SetActive(true);
        }
        else if(!_toggleTalentCopy.isOn  && !_toggleTalentInvis.isOn && _toggleAbility.gameObject.activeSelf)
        {
            _toggleAbility.gameObject.SetActive(false);
        }

        if(_toggleAbility.isOn && _toggleAbility.gameObject.activeSelf)
        {
            HandleToggleAbilityOn();
        }
        else if(_toggleAbility.isOn == false)
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

        if (GetComponent<Invisibility_PsionicaTalent3>().IsInvisibility)
        {
            _distance = 9 * 1.94f; //8 клеток
        }
        else
        {
            _distance = 5 * 1.94f; // 4 клетки
        }
    }

    private void HandleToggleAbilityOn()
    {
        if (_canDrawCircle && _player.GetComponent<MoveComponent>().IsSelect)
        {
            _drawCircle.Draw(_distance);
            _canDrawCircle = false;
        }

        _iconAbility.GetComponent<SpriteRenderer>().enabled = true;
        Color newColor = _iconAbility.GetComponent<SpriteRenderer>().color;
        newColor.a = 1f;
        _iconAbility.GetComponent<SpriteRenderer>().color = newColor;

        if (_newAbilityPrefab != null && _cursorIsActive == true)
        {
            Cursor.visible = false;
            _cursorIsActive = false;
        }

        if (_newAbilityPrefab == null && _cursorIsActive == false)
        {
            Cursor.visible = true;
            _cursorIsActive = true;
        }

        if (_target == null)
        {
            HandlePrefabVisibility();

            if (!_bleedCopied && !_markCopied)
            {
                HandleCopyAbility();
            }
            else
            {
                HandleTargetSelection();
            }
        }
        else
        {
            ApplyEffect();
        }
    }

    private void HandleToggleAbilityOff()
    {
        if (_blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
            _blinkCoroutine = null;
        }

        _iconAbility.GetComponent<SpriteRenderer>().enabled = false;
        _iconNoBlink.SetActive(false);

        if (_iconCoursor != null)
        {
            _iconCoursor.SetActive(false);
        }

        if(_cursorIsActive == false)
        {
            Cursor.visible = true;
            _cursorIsActive = true;
        }

        if (_drawCircle.lineRenderer && _drawCircle.lineRenderer.positionCount > 0 && _canDrawCircle == false)
        {
            _drawCircle.Clear();

            _canDrawCircle = true;
        }

        if (_newAbilityPrefab != null)
        {
            Destroy(_newAbilityPrefab);
        }

        _target = null;
        _isPrefab = false;
        _bleedCopied = false;
        _markCopied = false;

    }

    private void IncreasingPsionicsCreated()
    {
        if(_toggleTalentCopy.isOn && GetComponent<PoisonTalent>().PsionicaValue == 0)
        {
            GetComponent<PoisonTalent>().PsionicaValue = 2;
        }
        else if(!_toggleTalentCopy.isOn)
        {
            GetComponent<PoisonTalent>().PsionicaValue = 0;
        }
    }

    private void HandlePrefabVisibility()
    {
        // Создание префаба
        _abilityPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (_newAbilityPrefab == null && _isPrefab == false)
        {
            _newAbilityPrefab = Instantiate(_abilityPrefab, _abilityPosition, Quaternion.identity);
            _isPrefab = true;
        }
        else if (_newAbilityPrefab != null)
        {
            _newAbilityPrefab.transform.position = _abilityPosition;

            if(_iconCoursor == null)
            {
                _iconCoursor = _newAbilityPrefab.transform.Find("Circle").gameObject;
            }
        }
    }

    private void HandleCopyAbility()
    {
        // Копирование способностей

        Vector2 _targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(_targetPosition, Vector2.zero);
        if (hit.collider != null && hit.collider.CompareTag("Enemies") && hit.collider.gameObject != gameObject)
        {
            float _distanceToTarget = (hit.collider.gameObject.transform.position - _player.transform.position).magnitude;

            if (_distanceToTarget <= _distance)
            {
                if (_toggleTalentCopy.isOn && hit.collider.gameObject.GetComponentInChildren<BleedingDebaff>() != null)
                {
                    _copyTarget = hit.collider.gameObject;

                    _iconCoursor.SetActive(true);
                    _iconNoBlink.SetActive(true);

                    if (_blinkCoroutine == null)
                    {
                        _blinkCoroutine = StartCoroutine(Blink());
                    }

                    _bleedCopied = true;
                }
                else if (_toggleTalentInvis.isOn && hit.collider.gameObject.GetComponentInChildren<MarkOfPredator>() != null)
                {
                    _copyTarget = hit.collider.gameObject;

                    _iconCoursor.SetActive(true);
                    _iconNoBlink.SetActive(true);

                    if (_blinkCoroutine == null)
                    {
                        _blinkCoroutine = StartCoroutine(Blink());
                    }

                    _markCopied = true;
                }
            }
        }
    }

    private void HandleTargetSelection()
    {
        // Выбор врага

        Vector2 _targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(_targetPosition, Vector2.zero);
        Debug.Log(_copyTarget);
        if (hit.collider != null && hit.collider.gameObject != _copyTarget || hit.collider == null || _copyTarget == null)
        {
            _copyTarget = null;

            if (hit.collider != null && hit.collider.CompareTag("Enemies") && hit.collider.gameObject != gameObject)
            {
                float _distanceToTarget = (hit.collider.gameObject.transform.position - _player.transform.position).magnitude;

                if (_distanceToTarget <= _distance)
                {
                    _target = hit.collider.gameObject;

                    
                }
            }
        }
    }

    private void ApplyEffect()
    {
        if (_player.GetComponent<PsionicaMelee>().Psionica > 0)
        {
            if (_toggleTalentCopy.isOn && _bleedCopied)
            {
                if (_newAbilityPrefab != null)
                {
                    Destroy(_newAbilityPrefab);
                }

                _player.transform.Find("Abilities").gameObject.GetComponent<Bleeding>().StartBleed(_target, null);
                _player.GetComponent<PsionicaMelee>().UsePsionica(10);
                StartCoroutine(RechargeCoroutine());
            }
            else if(_toggleTalentInvis && _markCopied)
            {
                if (_newAbilityPrefab != null)
                {
                    Destroy(_newAbilityPrefab);
                }

                _newMarkEffectPrefab = Instantiate(_markEffect);
                _newMarkEffectPrefab.GetComponent<MarkOfPredator>().Player = _player;
                _newMarkEffectPrefab.transform.SetParent(_target.transform);

                _player.GetComponent<PsionicaMelee>().UsePsionica(30);
            }
        }
    }

    private IEnumerator Blink()
    {
        while (true && _player.GetComponent<MoveComponent>().IsSelect && _newAbilityPrefab != null)
        {
            // Затухание
            for (float t = 0f; t < 1; t += Time.deltaTime)
            {
                float normalizedTime = t / 1;
                float alpha = Mathf.Lerp(1f, 0f, normalizedTime);

                Color newColor = _iconAbility.GetComponent<SpriteRenderer>().color;
                newColor.a = alpha;
                _iconAbility.GetComponent<SpriteRenderer>().color = newColor;

                if (_newAbilityPrefab != null)
                {
                    Color newCoursorColor = _iconCoursor.GetComponent<SpriteRenderer>().color;
                    newCoursorColor.a = alpha;
                    _iconCoursor.GetComponent<SpriteRenderer>().color = newCoursorColor;
                }

                yield return null;
            }

            // Появление
            for (float t = 0f; t < 1; t += Time.deltaTime)
            {
                float normalizedTime = t / 1;
                float alpha = Mathf.Lerp(0f, 1f, normalizedTime);

                Color newColor = _iconAbility.GetComponent<SpriteRenderer>().color;
                newColor.a = alpha;
                _iconAbility.GetComponent<SpriteRenderer>().color = newColor;

                if (_newAbilityPrefab != null)
                {
                    Color newCoursorColor = _iconCoursor.GetComponent<SpriteRenderer>().color;
                    newCoursorColor.a = alpha;
                    _iconCoursor.GetComponent<SpriteRenderer>().color = newCoursorColor;
                }

                yield return null;
            }
        }
    }

    private IEnumerator RechargeCoroutine()
    {
        _abilityCooldownTime = 12;

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
