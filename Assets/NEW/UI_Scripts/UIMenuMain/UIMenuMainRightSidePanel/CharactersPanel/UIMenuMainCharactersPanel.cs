using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class UIMenuMainCharactersPanel : MonoBehaviour
{
    [ReadOnly,ShowInInspector]
    public UIMenuMainWindow Owner;
    
    [SerializeField] private UIMenuMainCharactersPanelItem _characterItem;
    [SerializeField] private RectTransform _itemsParent;
    
    private HeroComponent _currentHero;
    public HeroComponent CurrentHero => _currentHero;
    
    private List<UIMenuMainCharactersPanelItem> _characters = new();
    
    public void Show()
    {
        if(Owner == null) return;
        
        var charactersGroup = ServerManager.Instance.HeroList;

        foreach (var item in charactersGroup)
        {
            var character = Instantiate(_characterItem, _itemsParent);
            character.Owner = this;
            character.Fill(item);
            character.Selected += OnPlayerSelected;
            _characters.Add(character);
        }

        if (_currentHero == null)
        {
            _characters[0].Select();   
        }
    }

    void OnPlayerSelected(HeroComponent hero)
    {
        _currentHero = hero;
        Owner.SetHero(hero);
        ServerManager.Instance.SetPlayer(hero);
    }

    public void SetHero(HeroComponent hero)
    {
		Debug.Log(hero);

		_currentHero = hero;
    }
}
