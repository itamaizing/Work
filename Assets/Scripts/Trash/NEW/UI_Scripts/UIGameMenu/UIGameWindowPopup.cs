using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UIGameWindowPopup : MonoBehaviour
{
    [SerializeField] private UIMenuMainAttributesPanel _attributesPanel;
    [SerializeField] private UIMenuMainTalentsPanel _talentsPanel;
    [SerializeField] private PlayerIcon _playerIcon;
    [SerializeField] private MinionPanel _minionPanel;
    [SerializeField] private SkillPanel _skillPanel;
    [SerializeField] private SkillPanel _skillMinionPanel;
    [SerializeField] private SelectManager _selectManager;
    [SerializeField] private GameObject _settings;
    [SerializeField] private GameObject _teamStatistics;
    [SerializeField] private GameObject[] _forHide;
    [SerializeField] private GameObject _teamSource; //test

    private MinionSkillPanelController _minionSkillController;

    private HeroComponent _currentHero;
    private Character _currentCharacter;

    private void Awake()
    {
        InputHandler.ShowMenu += ShowSettings;
        InputHandler.ShowStatistics += ShowStatistics;
        _minionSkillController = new MinionSkillPanelController(_skillMinionPanel);
    }

    public void SwichAll(bool value)
    {
        if (value == false)
            foreach (var item in _forHide)
                item.SetActive(false);
        else
            foreach (var item in _forHide)
                item.SetActive(true);
    }

    private void TeamSourceSwich(bool value) => _teamSource.SetActive(value);

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

    private void ShowStatistics()
    {
        if (_teamStatistics.activeSelf)
        {
            _teamStatistics.SetActive(false);
        }
        else
        {
            _teamStatistics.SetActive(true);
        }
    }

    private void OnEnable()
    {
        _selectManager.UIVisibilityToggled += TeamSourceSwich;
        _selectManager.CharacterSelected += OnCharacterSelected;
        _selectManager.CharacterDeselected += OnCharacterDeselected;
    }

    private void OnDisable()
    {
        _selectManager.UIVisibilityToggled -= TeamSourceSwich;
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

        _currentCharacter.SpawnComponent.UnitAdded += OnMinionSpawned;
        _currentCharacter.SpawnComponent.UnitRemoved += OnMinionRemoved;
    }

    private void OnCharacterDeselected(Character character)
    {
        _playerIcon.OnCharacterDeselected(character);
        _minionPanel.OnCharacterDeselected(character);
        _skillPanel.OnCharacterDeselected(character);
        _attributesPanel.ShowHide(false);
        _attributesPanel.gameObject.SetActive(false);
        _talentsPanel.HidePanels();
        _talentsPanel.gameObject.SetActive(false);
        _skillMinionPanel.gameObject.SetActive(false);

        _currentCharacter.SpawnComponent.UnitAdded -= OnMinionSpawned;
        _currentCharacter.SpawnComponent.UnitRemoved -= OnMinionRemoved;
    }
    
    private void UpdateCharacterPanels()
    {
        if(_currentHero == null)
            return;
        
        _playerIcon.OnCharacterSelected(_currentHero);
        _minionPanel.OnCharacterSelected(_currentHero);
        _skillPanel.OnCharacterSelected(_currentHero);

        _attributesPanel.gameObject.SetActive(true);
        _attributesPanel.Show(_currentHero, false);
        _attributesPanel.ShowHide(true);

        _talentsPanel.gameObject.SetActive(true);
        _talentsPanel.Show(_currentHero.TalentManager, true);

        UpdateMinionSkills();

        _skillMinionPanel.gameObject.SetActive(true);
        _skillMinionPanel.SetHideUnusedButtons(true);
    }

    private void UpdateMinionSkills()
    {
        if (_currentCharacter == null) return;
        var spawn = _currentCharacter.SpawnComponent;
        if (spawn == null) return;

        _minionSkillController.Clear();
        foreach (var unit in spawn.Units)
            if (unit is MinionComponent { IsDead: false })
                _minionSkillController.RegisterMinion(unit);

        _skillMinionPanel.gameObject.SetActive(true);
        _skillMinionPanel.SetHideUnusedButtons(true);
    }

    private void OnMinionSpawned(Character character)
    {
        if (character == null || character.IsDead || character is not MinionComponent) return;
        StartCoroutine(RegisterMinionNextFrame(character));
    }

    private IEnumerator RegisterMinionNextFrame(Character character)
    {
        yield return null;
        if (character != null && !character.IsDead)
            _minionSkillController.RegisterMinion(character);
    }
    
    private void OnMinionRemoved(Character character)
    {
        _minionSkillController.UnregisterMinion(character);
    }
}
