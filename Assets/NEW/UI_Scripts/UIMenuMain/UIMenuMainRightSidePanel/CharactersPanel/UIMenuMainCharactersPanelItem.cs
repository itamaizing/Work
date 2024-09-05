using Mirror;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIMenuMainCharactersPanelItem : MonoBehaviour, IPointerEnterHandler,IPointerExitHandler
{
    public event UnityAction<HeroComponent> Selected;
    
    [ReadOnly,ShowInInspector]
    public UIMenuMainCharactersPanel Owner;
    
    [SerializeField] private Image _icon;

    private HeroComponent _currentHero;
    private Vector3 _increasedScale = new Vector3(1.3f, 1.3f, 1);
    private Vector3 _standardScale = new Vector3(1f, 1f, 1);
    
    public void Fill(HeroComponent hero)
    {
        _icon.sprite = hero.Data.Icon;
        _currentHero = hero;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = _increasedScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = _standardScale;
    }

    public void Select()
    {
        Selected?.Invoke(_currentHero);
    }
}
