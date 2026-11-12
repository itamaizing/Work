using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UIGameWindowPopup : MonoBehaviour
{
    [SerializeField] private UIMenuMainAttributesPanel _attributesPanel;
    [SerializeField] private UIMenuMainTalentsPanel _talentsPanel;
    [SerializeField] private GameObject _progressionPanel;
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
    private Character _hudHero;
    
    private HeroComponent _inspectedHero;

    private void Awake()
    {
        InputHandler.ShowMenu += ShowSettings;
        InputHandler.ShowStatistics += ShowStatistics;
        _minionSkillController = new MinionSkillPanelController(_skillMinionPanel);
        _progressionPanel.SetActive(false);
    }

    public void SwichAll(bool value)
    {
        foreach (var item in _forHide)
        {
            if (item == _progressionPanel)
            {
                if (value && _inspectedHero != null) ShowHeroInfo(_inspectedHero);
                else item.SetActive(false);
            }
            else
                item.SetActive(value);
        }
    }

    private void TeamSourceSwich(bool value) => _teamSource.SetActive(value);

    private void ShowSettings()
    {
        Debug.Log(_settings);   //почему-то без дерганья _settings, они не находятся
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
            _teamStatistics.GetComponent<TeamSource>().UpdateInfo();
        }
    }

    private void OnEnable()
    {
        _selectManager.UIVisibilityToggled += TeamSourceSwich;
        _selectManager.CharacterSelected += OnCharacterSelected;
        _selectManager.CharacterDeselected += OnCharacterDeselected;

        PlayerIconMain.HeroPortraitClicked += OnHeroPortraitClicked;
    }

    private void OnDisable()
    {
        _selectManager.UIVisibilityToggled -= TeamSourceSwich;
        _selectManager.CharacterSelected -= OnCharacterSelected;
        _selectManager.CharacterDeselected -= OnCharacterDeselected;

        PlayerIconMain.HeroPortraitClicked -= OnHeroPortraitClicked;

        _hudHero = null;
        if (_playerIcon != null) _playerIcon.OnCharacterSelected(null);
    }

    private void LateUpdate()
    {
        var hero = Character.Local;
        if (hero == null || !hero.IsInitialized)
            hero = null;

        if (ReferenceEquals(_hudHero, hero)) return;

        _hudHero = hero;
        _playerIcon.OnCharacterSelected(hero);
    }
    
    private void OnDestroy()
    {
        _minionSkillController?.Dispose();
    }
    
    private void OnHeroPortraitClicked(HeroComponent hero, bool isAllied)
    {
        if (!isAllied || hero == null) return;
        if (_inspectedHero == hero && _progressionPanel.activeSelf)
        {
            _inspectedHero = null;
            _progressionPanel.SetActive(false);
            return;
        }

        _inspectedHero = hero;
        ShowHeroInfo(hero);
    }

    private void ShowHeroInfo(HeroComponent hero)
    {
        bool canEdit = hero == Character.Local;
        _progressionPanel.SetActive(true);
        _attributesPanel.gameObject.SetActive(true);
        _talentsPanel.gameObject.SetActive(true);
        _attributesPanel.Show(hero, isMenu: false, isInteractable: canEdit);
        _talentsPanel.Show(hero.TalentManager, isGameUI: true, isInteractable: canEdit);
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
        _minionPanel.OnCharacterDeselected(character);
        _skillPanel.OnCharacterDeselected(character);
        _skillMinionPanel.gameObject.SetActive(false);

        _currentCharacter.SpawnComponent.UnitAdded -= OnMinionSpawned;
        _currentCharacter.SpawnComponent.UnitRemoved -= OnMinionRemoved;
    }

    private void UpdateCharacterPanels()
    {
        if (_currentHero == null) return;

        _minionPanel.OnCharacterSelected(_currentHero);
        _skillPanel.OnCharacterSelected(_currentHero);

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
