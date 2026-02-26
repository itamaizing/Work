using UnityEngine;

public class UIMenuMainWindow : MonoBehaviour
{
    [SerializeField] private UIMenuMainAbilitiesPanel _abilitiesPanel;
    [SerializeField] private SkillPanel _skillPanel;
    [SerializeField] private UIMenuMainAttributesPanel _attributesPanel;
    [SerializeField] private UIMenuMainTalentsPanel _talentsPanel;
    [SerializeField] private UIMenuMainCharactersPanel _charactersPanel;
    [SerializeField] private UIMenuMainGameTypesPanel _gameTypesPanel;
    [SerializeField] private UIMenuMainSavesPanel _savesPanel;
    [SerializeField] private UIMenuMainPlayerInfoPanel _infoPanel;

    private void Start()
    {
        Show();
        _abilitiesPanel.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        _charactersPanel.OnHeroChanged += SetHero;
        _savesPanel.OnSelect += SetHeroSaveIndex;
    }

    private void OnDisable()
    {
        _charactersPanel.OnHeroChanged -= SetHero;
        _savesPanel.OnSelect -= SetHeroSaveIndex;
    }

    public void UI_StartClient()
    {
        ServerManager.Instance.StartClient();
    }

    void Show()
    {
		_charactersPanel.Show();
        _gameTypesPanel.Show();
        _savesPanel.Show();
        
        UpdateCharacterPanels();
    }

    public void SetHero(HeroComponent hero)
    {
        var currentHero = hero;

		SaveManager.Instance.SetHero(currentHero);
        ServerManager.Instance.SetPlayer(hero);

        UpdateCharacterPanels();
    }

    public void SetHeroSaveIndex(int index)
    {
        SaveManager.Instance.SetSaveIndex(index);
        SaveManager.Instance.LoadAllData();

        UpdateCharacterPanels();
    }

    private HeroComponent GetHero()
    {
        return _charactersPanel.CurrentHero;
    }

    private void UpdateCharacterPanels()
    {
        var hero = GetHero();

        //_abilitiesPanel.Show(hero.Abilities);

        hero.TalentManager.Initialize(hero.LVL);

        if(_skillPanel != null)
            _skillPanel.FillMenu(hero.Abilities, hero);
        
        _attributesPanel.Show(hero);

        _talentsPanel.Show(hero.TalentManager, false);
    }
}
