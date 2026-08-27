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

    private HeroComponent _currentHero;
    public HeroComponent CurrentHero => _currentHero;

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
        var snapshot = HeroProgressSnapshotBuilder.Build(_currentHero, _attributesPanel);
        MPNetworkManager.Instance.PendingHeroProgressSnapshot = snapshot;

        ServerManager.Instance.StartClient();
    }

    void Show()
    {
        if (Application.isBatchMode ||
            SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null ||
            !Application.isPlaying)
        {
            return;
        }
        
        string userKey = MPNetworkManager.Instance.UserID.ToString();
        BottleUserManager.Instance.SetUser(userKey);
        SaveManager.Instance.LoadBottles(userKey,
            onLoaded: (bottles, volume) => BottleUserManager.Instance.ApplyLoadedBottleData(bottles, volume),
            onFailed: null);

        _charactersPanel.Show();
        _gameTypesPanel.Show();
        _savesPanel.Show();

        UpdateCharacterPanels();
        SaveManager.Instance.LoadHeroProgress(_attributesPanel, isStillCurrent: () => GetHero() == _currentHero, onComplete: () => { if (GetHero() == _currentHero) UpdateCharacterPanels(); });
    }


    public void SetHero(HeroComponent hero)
    {
        _currentHero = hero;

        SaveManager.Instance.SetHero(_currentHero);
        ServerManager.Instance.SetPlayer(_currentHero);

        UpdateCharacterPanels();
        SaveManager.Instance.LoadHeroProgress(_attributesPanel, isStillCurrent: () => GetHero() == _currentHero, onComplete: () => { if (GetHero() == _currentHero) UpdateCharacterPanels(); });
    }

    public void SetHeroSaveIndex(int index)
    {
        SaveManager.Instance.SetSaveIndex(index);
        SaveManager.Instance.LoadHeroProgress(_attributesPanel,
            isStillCurrent: () => true,
            onComplete: UpdateCharacterPanels);
    }

    public HeroComponent GetHero()
    {
        return _charactersPanel.CurrentHero;
    }
    
    private void TryLoadServerArrangement(HeroComponent hero)
    {
        if (hero == null) return;
        if (MPNetworkManager.Instance == null || !MPNetworkManager.Instance.IsServer()) return;
    }

    private void UpdateCharacterPanels()
    {
        var hero = GetHero();

        //_abilitiesPanel.Show(hero.Abilities);

        //hero.TalentManager.Initialize(hero.LVL);

        _talentsPanel.Show(hero.TalentManager, false);

        if (_skillPanel != null)
            _skillPanel.FillMenu(hero.Abilities, hero);
        
        _attributesPanel.Show(hero);
    }
}
