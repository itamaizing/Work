using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AbilityIcon : MonoBehaviour , IPointerEnterHandler , IPointerExitHandler
{
    [SerializeField] private Image _abilityIcon;
    [SerializeField] private Image _boxFrame;
    [SerializeField] private Blink _autoAttackBoxFrame;
    [SerializeField] private FillAmountOverTime _cooldown;
    [SerializeField] private TextMeshProUGUI _chargeCounter;
    [SerializeField] private GameObject _abilityNameBox;
    [SerializeField] private TextMeshProUGUI _name;
    [SerializeField] private TextMeshProUGUI _description;

    private FillAmountOverTime _castLine;
    private Ability _ability;

    public void Init(Ability ability, FillAmountOverTime castLine)
    {
        _ability = ability;
        _abilityIcon.sprite = ability.Icon;
        _name.text = ability.Name;
        _description.text = ability.Description;
        _castLine = castLine;

        if (ability.IsUseCharges)
        {
            ability.CurrentChargeChange += OnCurrentChargeText;
            _chargeCounter.enabled = true;
            OnCurrentChargeText(ability.Chargers);
        }
        if (ability.IsStreaming)
        {
            ability.StartStreaming += OnStartStreaming;
            ability.StopStreaming += OnStopStreaming;
        }
        ability.StartCastDeley += OnStartCastDeley;
        ability.StopCastDeley += OnStopCastDeley;

        ability.CooldownStarted += OnStartCooldown;
    }

    public void OnCurrentChargeText(int value)
    {
        if (value > 0)
            _chargeCounter.color = Color.green;
        else
            _chargeCounter.color = Color.red;

        _chargeCounter.text = value.ToString();
    }

    public void OnStartCooldown(float dutarion)
    {
        _cooldown.StartFill(dutarion, 1, 0, false);
        _cooldown.gameObject.SetActive(true);
    }

    public void Selected()
    {
        _boxFrame.color = Color.green;
    }

    public void Deselected()
    {
        _boxFrame.color = Color.white;
    }

    public void AutoAttackSelected()
    {
        _autoAttackBoxFrame.gameObject.SetActive(true);
        _autoAttackBoxFrame.StartBlink(.5f);
    }

    public void AutoAttackDeselected()
    {
        _autoAttackBoxFrame.gameObject.SetActive(false);
        _autoAttackBoxFrame.StopBlink();
    }

    public void DestroyIcon()
    {
        Destroy(gameObject);
    }

    private void OnStartStreaming(float time)
    {
        _castLine.gameObject.SetActive(true);
        _castLine.StartFill(time, 1, 0);
        StartCoroutine(CastLineCoroutine());
    }

    private void OnStopStreaming()
    {
        _castLine.gameObject.SetActive(false);
        _castLine.Stop();
    }

    private void OnStartCastDeley(float time)
    {
        _castLine.gameObject.SetActive(true);
        _castLine.StartFill(time);
        StartCoroutine(CastLineCoroutine());
    }

    private void OnStopCastDeley()
    {
        _castLine.gameObject.SetActive(false);
        _castLine.Stop();
    }

    private IEnumerator CastLineCoroutine()
    {
        while (_castLine.enabled)
        {
            _castLine.transform.position = _ability.transform.position + new Vector3(0, -1.7f, 0);

            yield return null;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _abilityNameBox.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _abilityNameBox.SetActive(false);
    }
}
