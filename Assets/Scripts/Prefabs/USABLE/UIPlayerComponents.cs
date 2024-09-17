using UnityEngine;

public class UIPlayerComponents : MonoBehaviour
{
    [SerializeField] private Character _character;
    [SerializeField] private SelectedCircle CircleSelect;
    [SerializeField] private MinimapMarker MarkersSelect;
    [SerializeField] private FillAmountOverTime _castLine;

    public Transform DamageSpawn;
    public PopupTextPrefab PopupText;
    private PopupTextPrefab popupTextPrefab;

    private Color _magDamageColor = Color.blue;
    private Color _physDamageColor = Color.red;

    /* public void Initialize(PlayerAbilities playerAbilities,MoveComponent playerMove,StaminaComponent staminaComponent , HealthComponent healthComponent)
     {
         playerAbilities.Initialize(playerMove, staminaComponent, healthComponent);
     }
     */ //Why is initialization of this component necessary at all? Moreover, the UI should not initialize the logic
    private void Awake()
    {
        _character.Health.DamageTaken += OnDamageTaken;

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
        if(value is > 0 and < 1)
        {
            value = 1;
        }
        popupTextPrefab = Instantiate(PopupText, DamageSpawn.position, Quaternion.identity,transform);
        popupTextPrefab.PopupText.text = (value > 0 ? "+" : "") + value.ToString("0.0");
        popupTextPrefab.StartColor = startColor;
        popupTextPrefab.EndColor = endColor;
    }

    public void ShowPopupText(string text, Color startColor, Color endColor) //������������ ��� �������
    {
        popupTextPrefab = Instantiate(PopupText, DamageSpawn.position, Quaternion.identity,transform);
        popupTextPrefab.PopupText.text = text;
        popupTextPrefab.StartColor = startColor;
        popupTextPrefab.EndColor = endColor;
    }

    private void OnDamageTaken(float value, DamageType damageType)
    {
        switch (damageType)
        {
            case DamageType.Magical:
                ShowPopupValue(-value, _magDamageColor, _magDamageColor);
                break;

            case DamageType.Physical:
                ShowPopupValue(-value, _physDamageColor, _physDamageColor);
                break;

            default:
                ShowPopupValue(-value, _physDamageColor, _physDamageColor);
                break;
        }
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
