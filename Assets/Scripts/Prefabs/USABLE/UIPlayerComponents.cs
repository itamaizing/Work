using System.Collections;
using UnityEngine;

public class UIPlayerComponents : MonoBehaviour
{
    [SerializeField] private Character _character;
    [SerializeField] private SelectedCircle CircleSelect;
    [SerializeField] private MinimapMarker MarkersSelect;
    [SerializeField] private FillAmountOverTime _castLine;

    public Transform DamageSpawn;
    public Transform RegenSpawn;
    public PopupTextPrefab PopupText;
    private PopupTextPrefab popupTextPrefab;

    private Color _shieldColor = Color.blue;
    private Color _physDamageColor = Color.red;
    private Color _regenColor = Color.green;

    private float popupSpawnDelay = 0.2f;
    private bool canSpawnPopup = true;

    /* public void Initialize(PlayerAbilities playerAbilities,MoveComponent playerMove,StaminaComponent staminaComponent , HealthComponent healthComponent)
     {
         playerAbilities.Initialize(playerMove, staminaComponent, healthComponent);
     }
     */ //Why is initialization of this component necessary at all? Moreover, the UI should not initialize the logic
    private void Awake()
    {
        _character.Health.DamageTaken += OnDamageTaken;
        _character.Health.ShieldDamageTaken += OnShieldDamageTaken;
        _character.Health.HealthRegenerated += OnHealthRegenerated;
        _character.Health.OnShieldAdd += OnShieldAdded;

        foreach (var ability in _character.Abilities.Abilities)
        {
            ability.CastStreamStarted += OnStartStreaming;
            ability.Canceled += OnStopStreaming;

            ability.CastDeleyStarted += OnStartCastDeley;
            ability.Canceled += OnStopCastDeley;
        }
    }

    public void ChangeSelection(bool isSelect)
    {
        CircleSelect.IsActive = isSelect;
        MarkersSelect.IsActive = isSelect;
    }

    public void ShowPopupValue(float value, Color startColor, Color endColor)
    {
        if (value is > 0 and < 1)
        {
            value = 1;
        }
        if (canSpawnPopup)
        {
            StartCoroutine(SpawnPopupWithDelay((value > 0 ? "+" : "") + value.ToString("0.0"), startColor, endColor));
        }
    }

    public void ShowPopupText(string text, Color startColor, Color endColor)
    {
        if (canSpawnPopup)
        {
            StartCoroutine(SpawnPopupWithDelay(text, startColor, endColor));
        }
    }

    private IEnumerator SpawnPopupWithDelay(string text, Color startColor, Color endColor)
    {
        canSpawnPopup = false;
        popupTextPrefab = Instantiate(PopupText, DamageSpawn.position, Quaternion.identity, transform);
        popupTextPrefab.PopupText.text = text;
        popupTextPrefab.StartColor = startColor;
        popupTextPrefab.EndColor = endColor;

        yield return new WaitForSeconds(popupSpawnDelay);
        canSpawnPopup = true;
    }

    public void ShowPopupValueRegen(float value, Color startColor, Color endColor)
    {
        if (value is > 0 and < 1)
        {
            value = 1;
        }
        if (canSpawnPopup)
        {
            StartCoroutine(SpawnPopupWithDelayRegen((value > 0 ? "+" : "-") + value.ToString("0.0"), startColor, endColor));
        }
    }

    public void ShowPopupTextRegen(string text, Color startColor, Color endColor)
    {
        if (canSpawnPopup)
        {
            StartCoroutine(SpawnPopupWithDelayRegen(text, startColor, endColor));
        }
    }

    private IEnumerator SpawnPopupWithDelayRegen(string text, Color startColor, Color endColor)
    {
        canSpawnPopup = false;
        popupTextPrefab = Instantiate(PopupText, RegenSpawn.position, Quaternion.identity, transform);
        popupTextPrefab.PopupText.text = text;
        popupTextPrefab.StartColor = startColor;
        popupTextPrefab.EndColor = endColor;

        yield return new WaitForSeconds(popupSpawnDelay);
        canSpawnPopup = true;
    }

    private void OnHealthRegenerated(float regenAmount)
    {
        ShowPopupValueRegen(regenAmount, _regenColor, _regenColor);
    }

    private void OnDamageTaken(float value, DamageType damageType, Skill skill)
    {
        ShowPopupValue(-value, _physDamageColor, _physDamageColor);
    }

    private void OnShieldDamageTaken(float damageTaken, DamageType damageType, Skill skill)
    {
        ShowPopupValue(-damageTaken, _shieldColor, _shieldColor);
    }

    private void OnShieldAdded(float shieldValue)
    {
        ShowPopupValue(shieldValue, _shieldColor, _shieldColor);
    }

    private void OnStartStreaming(float time)
    {
        _castLine.gameObject.SetActive(true);
        _castLine.StartFill(time, 1, 0);
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
    }

    private void OnStopCastDeley()
    {
        _castLine.gameObject.SetActive(false);
        _castLine.Stop();
    }
}
