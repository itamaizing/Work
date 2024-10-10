using UnityEngine;

public class UIMenuMainWindow : MonoBehaviour
{
    [SerializeField] private UIMenuMainAbilitiesPanel _abilitiesPanel;
    [SerializeField] private UIMenuMainAttributesPanel _attributesPanel;
    [SerializeField] private UIMenuMainTalentsPanel _talentsPanel;
    [SerializeField] private UIMenuMainCharactersPanel _charactersPanel;
    [SerializeField] private UIMenuMainGameTypesPanel _gameTypesPanel;
    [SerializeField] private UIMenuMainSavesPanel _savesPanel;

    private void Start()
    {
        Show();
    }

    public void UI_StartClient()
    {
        ServerManager.Instance.StartClient();
    }

    void Show()
    {
		_charactersPanel.Owner = this;
		_charactersPanel.Show();

        _gameTypesPanel.Owner = this;
        _gameTypesPanel.Show();

        _savesPanel.Owner = this;
        _savesPanel.Show();
        
        UpdateCharacterPanels();
    }

    public void SetHero(HeroComponent hero)
    {
        var currentHero = hero;

		SaveManager.Instance.SetHero(currentHero);
        currentHero.Initialize();
        UpdateCharacterPanels();
    }

    public void SetHeroSaveIndex(int index)
    {
        SaveManager.Instance.SetSaveIndex(index);
        SaveManager.Instance.LoadAttributes();
        SaveManager.Instance.LoadTalents();

        UpdateCharacterPanels();
    }

    public HeroComponent GetHero()
    {
        return _charactersPanel.CurrentHero;
    }

    private void UpdateCharacterPanels()
    {
        _abilitiesPanel.Owner = this;
        _abilitiesPanel.Show();

        _attributesPanel.Owner = this;
        _attributesPanel.Show();

        _talentsPanel.Owner = this;
        _talentsPanel.Show();
    }

    public void UpdateAttributes()
    {
        _attributesPanel.UpdateAttributesPoints();
    }
}
