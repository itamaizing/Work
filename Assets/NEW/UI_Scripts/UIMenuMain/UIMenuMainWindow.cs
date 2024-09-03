using UnityEngine;

public class UIMenuMainWindow : MonoBehaviour
{
    [SerializeField] private UIMenuMainAbilitiesPanel _abilitiesPanel;
    [SerializeField] private UIMenuMainAttributesPanel _attributesPanel;
    [SerializeField] private UIMenuMainTalentsPanel _talentsPanel;
    [SerializeField] private UIMenuMainCharactersPanel _charactersPanel;
    [SerializeField] private UIMenuMainGameTypesPanel _gameTypesPanel;

    private void Start()
    {
        Show();
    }

    public void UI_StartClient()
    {
        MultiplayerManager.Instance.StartClient();
    }

    void Show()
    {
        _charactersPanel.Owner = this;
        _charactersPanel.Show();

        _gameTypesPanel.Owner = this;
        _gameTypesPanel.Show();
        
        UpdateCharacterPanels();
    }

    public void SetHero(HeroComponent hero)
    {
        var currentHero = hero;
        
        /*var heroData = SaveManager.Instance.SelectHero(hero.Data.ID);
        
        if (heroData != null)
        {
            currentHero.Initialize();
        }
        else
        {
            SaveManager.Instance.AddHeroToSave(hero);
            currentHero.Initialize();
        }*/
        currentHero.Initialize();
        UpdateCharacterPanels();
    }

    public HeroComponent GetHero()
    {
        return _charactersPanel.CurrentHero;
    }

    public void UpdateCharacterPanels()
    {
        _abilitiesPanel.Owner = this;
        _abilitiesPanel.Show();

        _attributesPanel.Owner = this;
        _attributesPanel.Show();

        _talentsPanel.Owner = this;
        _talentsPanel.Show();
    }
}
