using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIMenuMainAbilitiesPanelItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [ReadOnly,ShowInInspector]
    public UIMenuMainAbilitiesPanel Owner;
    
    [SerializeField] private Image _icon;

    [SerializeField] private UITwoStates frameState;
    
    
    private Skill _ability;
    public void Fill(Skill ability)
    {
        _ability = ability;
        _icon.sprite = _ability.Icon;
    }

    public void Destroy()
    {
        Owner.HideTooltip();
        Destroy(gameObject);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Owner == null) return;

        frameState.isActive = true;
        
        Owner.ShowTooltip(_ability, (Vector2)transform.position - new Vector2(-10,10));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (Owner == null) return;
        
        frameState.isActive = false;
        
        Owner.HideTooltip();
    }
}
