using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class FiveReversPolarity : MonoBehaviour
{
    public GameObject IconAbility;
    public Toggle ToggleAbility;
    public GameObject CurrentAbilitiesPanel;
    public GameObject Abilities;
    public GameObject[] Toggles;
    public GameObject CastPrefab;
    public GameObject ManaCost;

    private bool _canCast = true;
    private bool _isHealing = false;
    private bool _isGlobalCooldown;
    private GameObject _newCastPrefab;
    private bool _isEnabled = false;
    private Coroutine _coroutine;

    private void OnEnable()
    {
        GetComponent<ThreeRangeHeal>().ThirdAbilityEvent += HandleHealingEvent;

        GetComponent<OneRangeAttack>().FirstAbilityEvent += HandleOtherEvent;
        GetComponent<TwoRangeProtection>().SecondAbilityEvent += HandleOtherEvent;
        GetComponent<FourRangeRecovery>().FourthAbilityEvent += HandleOtherEvent;
        GetComponent<DarkTwoRangeProtection>().SecondDarkAbilityEvent += HandleOtherEvent;
        GetComponent<DarkThreeRangeHeal>().DarkThirdAbilityEvent += HandleOtherEvent;
        GetComponent<DarkFourRangeRecovery>().DarkFourthAbilityEvent += HandleOtherEvent;

    }

    private void OnDisable()
    {
        GetComponent<ThreeRangeHeal>().ThirdAbilityEvent -= HandleHealingEvent;

        GetComponent<OneRangeAttack>().FirstAbilityEvent -= HandleOtherEvent;
        GetComponent<TwoRangeProtection>().SecondAbilityEvent -= HandleOtherEvent;
        GetComponent<FourRangeRecovery>().FourthAbilityEvent -= HandleOtherEvent;
        GetComponent<DarkTwoRangeProtection>().SecondDarkAbilityEvent -= HandleOtherEvent;
        GetComponent<DarkThreeRangeHeal>().DarkThirdAbilityEvent -= HandleOtherEvent;
        GetComponent<DarkFourRangeRecovery>().DarkFourthAbilityEvent -= HandleOtherEvent;
    }

    private void HandleHealingEvent(float value)
    {
        _isHealing = true;
    }
    private void HandleOtherEvent(float value)
    {
        _isHealing = false;
        if (ToggleAbility.isOn)
        {
            ToggleAbility.isOn = false;
        }
    }



    void Update()
    {
        if (ToggleAbility.gameObject.activeSelf && Input.GetKeyDown(KeyCode.Alpha5) && transform.parent.GetComponent<PlayerMove>().IsSelect && ToggleAbility.enabled)
        {
            if (ToggleAbility.isOn)
            {
                ToggleAbility.isOn = false;
            }
            else
            {
                ToggleAbility.isOn = true;
            }
        }

        if (ToggleAbility.isOn == true)
        {
            _isEnabled = false;

            IconAbility.GetComponent<SpriteRenderer>().enabled = true;
            if (_canCast && !_isHealing)
            {
                _coroutine = StartCoroutine(Cast(1.5f));
            }
            else if (_canCast && _isHealing)
            {
               StartDarkBeginning(0);
            }

        }
        else
        {
            if(!_isEnabled)
            {
                for (int i = 0; i < Toggles.Length; i++)
                {
                    Toggles[i].SetActive(true);
                }
                _isEnabled = true;
            }
            CurrentAbilitiesPanel.SetActive(false);
            _canCast = true;
            IconAbility.GetComponent<SpriteRenderer>().enabled = false;
        }

        if(_coroutine != null)
        {
            ToggleAbility.enabled = false;
        }
    }
    private IEnumerator Cast(float castTime)
    {
        _canCast = false;
        transform.parent.GetComponent<PlayerMove>().CanMove = false;
        CreateCastPrefab(castTime);
        if (!_isGlobalCooldown)
        {
            Abilities.GetComponent<GlobalCooldown>().StartGlobalCooldown();
            _isGlobalCooldown = true;
        }

        for (int i = 0; i < Abilities.transform.childCount; i++)
        {
            GameObject childObject = Abilities.transform.GetChild(i).gameObject;

            Toggle toggle = childObject.GetComponent<Toggle>();

            if (toggle != null)
            {
                toggle.enabled = true;
            }
        }

        ManaCost.SetActive(true);
        ManaCost.GetComponent<VisualManaCost>().CheckManaCost();
        ManaCost.transform.localScale = new Vector2(2f, ManaCost.gameObject.transform.localScale.y);

        yield return new WaitForSeconds(castTime);

        ManaCost.SetActive(false);
       transform.parent.GetComponent<PlayerMove>().CanMove = true;

        ToggleAbility.enabled = true;

        StartDarkBeginning(20);
    }
    private void CreateCastPrefab(float time)
    {

        Vector2 newVector = new Vector2(transform.position.x, transform.position.y - 1);
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

        while (elapsedTime < duration)
        {
            targetTransform.localScale = Vector3.MoveTowards(initialScale, targetScale, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        targetTransform.localScale = targetScale;
    }
    private void StartDarkBeginning(int manaValue)
    {
        _coroutine = null;
        ToggleAbility.enabled = true;
        _canCast = false;
        transform.parent.GetComponent<ManaPlayer>().UseMana(manaValue);
        CurrentAbilitiesPanel.SetActive(true);
        for (int i = 0; i < Toggles.Length; i++)
        {
            Toggles[i].SetActive(false);
        }
        _isGlobalCooldown = false;
    }
}
