using UnityEngine;

public class UIPlayerComponents : MonoBehaviour
{
    [SerializeField] private SelectedCircle CircleSelect;
    [SerializeField] private MinimapMarker MarkersSelect;
    
    public Transform DamageSpawn;
    public PopupTextPrefab PopupText;
    private PopupTextPrefab popupTextPrefab;
    
    public void Initialize(PlayerAbilities playerAbilities,MoveComponent playerMove,StaminaComponent staminaComponent , HealthComponent healthComponent)
    {
        playerAbilities.Initialize(playerMove, staminaComponent, healthComponent);
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
}
