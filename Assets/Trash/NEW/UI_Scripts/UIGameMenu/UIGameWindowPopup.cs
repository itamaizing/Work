using System;
using UnityEngine;

public class UIGameWindowPopup : MonoBehaviour
{
    [SerializeField] private UIMenuMainAttributesPanel _attributesPanel;
    [SerializeField] private UIMenuMainTalentsPanel _talentsPanel;
    [SerializeField] private PlayerIcon _playerIcon;
    [SerializeField] private MinionPanel _minionPanel;
    [SerializeField] private SkillPanel _skillPanel;
    [SerializeField] private SelectManager _selectManager;
    [SerializeField] private GameObject _settings;

    private HeroComponent _currentHero;
    private Character _currentCharacter;

    private void Awake()
    {
        InputHandler.ShowMenu += ShowSettings;
    }

    private void ShowSettings()
    {
        if (_settings.activeSelf)
        {
            _settings.SetActive(false);
        }
        else
        {
            _settings.SetActive(true);
        }
    }

    private void OnEnable()
    {
        _selectManager.CharacterSelected += OnCharacterSelected;
        _selectManager.CharacterDeselected += OnCharacterDeselected;
    }

    private void OnDisable()
    {
        _selectManager.CharacterSelected -= OnCharacterSelected;
        _selectManager.CharacterDeselected -= OnCharacterDeselected;
    }
    
    private void OnCharacterSelected(Character character)
    {
        _currentCharacter = character;

        if (character is not HeroComponent hero)
        {
            UpdateCharacterPanels();
            return;
        }
        
        _currentHero = hero;
        SaveManager.Instance.SetHero(_currentHero);
        UpdateCharacterPanels();
    }

    private void OnCharacterDeselected(Character character)
    {
        _playerIcon.OnCharacterDeselected(character);
        _minionPanel.OnCharacterDeselected(character);
        _skillPanel.OnCharacterDeselected(character);
        _attributesPanel.ShowHide(false);
        _talentsPanel.HidePanels();
    }
    
    private void UpdateCharacterPanels()
    {
        if(_currentHero == null)
            return;
        
        _playerIcon.OnCharacterSelected(_currentHero);
        _minionPanel.OnCharacterSelected(_currentHero);
        _skillPanel.OnCharacterSelected(_currentCharacter);
        _attributesPanel.Show(_currentHero.Data.Attributes);
        _talentsPanel.Show(_currentHero.TalentManager, true);
    }
}
