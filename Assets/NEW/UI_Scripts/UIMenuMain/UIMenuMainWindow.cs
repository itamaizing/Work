using System.Collections.Generic;
using UnityEngine;

public class UIMenuMainWindow : MonoBehaviour
{
    [SerializeField] private UIMenuMainAbilitiesPanel _abilitiesPanel;
    [SerializeField] private UIMenuMainAttributesPanel _attributesPanel;
    [SerializeField] private UIMenuMainTalentsPanel _talentsPanel;
    [SerializeField] private UIMenuMainCharactersPanel _charactersPanel;
    [SerializeField] private UIMenuMainGameTypesPanel _gameTypesPanel;
    [SerializeField] private UIMenuMainSavesPanel _savesPanel;
    [SerializeField] private GameObject _uIMenuMainRightPanel;
    [SerializeField] private List<GameObject> _otherUIs;

	[SerializeField] private SelectManager _selectManager;

	private void Start()
    {
        Show();
        _selectManager.CharacterSelected += OnCharacterSelected;
        InputHandler.ShowMenu += SwithActiveAtriutTalantUI;
    }

    public void UI_StartClient()
    {
        ServerManager.Instance.StartClient();
        DisableUI();
    }

    public void DisableUI()
    {
        _uIMenuMainRightPanel.SetActive(false);
        gameObject.SetActive(false);

        foreach (var item in _otherUIs)
        {
            item.SetActive(true);
        }
    }

    public void EnableAtriutTalantUI()
    {
        gameObject.SetActive(true);
    }

    public void SwithActiveAtriutTalantUI()
    {
        gameObject.SetActive(!gameObject.active);
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

    private void OnCharacterSelected(Character character)
    {
        if (character is not HeroComponent component) return;
        
        _charactersPanel.SetHero(component);
        SaveManager.Instance.SetHero(component);
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
