using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SkillPanel : MonoBehaviour
{
    [SerializeField] private List<RebindUI> _rebindsUI;
    [SerializeField] private SkillIcon[] _skillIcons;

    [SerializeField] private DraggableIcon _skillIconPref;
    [SerializeField] private FillAmountOverTime _castLine;
    [SerializeField] private QueuePanel _queuePanel;
    [SerializeField] private SelectManager _selectManager;

    private List<DraggableIcon> _skills;
    private Character _currentCharacter;
    private SkillManager _playerAbilities;
    private bool _isActive;
    private bool _isSelect;

    private void Start()
    {
        UpdateKeys();

        foreach (var item in _rebindsUI)
        {
            item.updateBindingUIEvent.AddListener(OnRebindSpellKeys);
        }
        _selectManager.CharacterSelected += OnCharacterSelected;
        _selectManager.CharacterDeselected += OnCharacterDeselected;

        for (int i = 0; i < _skillIcons.Length; i++)
        {
            _skillIcons[i].Init(i, _castLine);
            _skillIcons[i].CurrentSkillChenged += SkillChenged;
        }

        InputHandler.OnCast += SelectSkill;
    }

    private void OnDestroy()
    {
        _selectManager.CharacterSelected -= OnCharacterSelected;
        _selectManager.CharacterDeselected -= OnCharacterDeselected;
    }

    public void Fill(SkillManager abilities)
    {
        if (_playerAbilities != null)
        {
            _playerAbilities.SkillSelected -= OnAbilitySelected;
            _playerAbilities.SkillDeselected -= OnAbilityDeselected;
            _playerAbilities.SkillAdded -= OnSkillAdded;
            _playerAbilities.SkillRemoved -= OnSkillRemoved;
        }

        _playerAbilities = abilities;

        for (int i = 0; i < _playerAbilities.SelectedSkills.Length; i++)
        {
            var icon = Instantiate(_skillIconPref, _skillIcons[i].transform);
            icon.Init(_playerAbilities.SelectedSkills[i]);
            _skills.Add(icon);
        }

        _playerAbilities.SkillSelected += OnAbilitySelected;
        _playerAbilities.SkillDeselected += OnAbilityDeselected;
        _playerAbilities.SkillAdded += OnSkillAdded;
        _playerAbilities.SkillRemoved += OnSkillRemoved;
    }

    private void SkillChenged(int index, Skill skill)
    {
        _playerAbilities.SelectedSkills[index] = skill;
    }

    private void OnCharacterSelected(Character character)
    {
        if (character != null && character != _currentCharacter)
        {
            gameObject.SetActive(true);
            _currentCharacter = character;
            Fill(_currentCharacter.Abilities);
            _queuePanel.Init(character.Abilities.SkillQueue);
        }
    }

    private void OnCharacterDeselected(Character character)
    {
        if (character != null && character == _currentCharacter)
        {
            ClearPanel();

            gameObject.SetActive(false);
            _currentCharacter = null;
        }
    }

    private void ClearPanel()
    {
        foreach (var item in _skills)
        {
            Destroy(item.gameObject);
        }
        _skills.Clear();
    }

    private void OnAbilitySelected(int index)
    {
        _skillIcons[index].Selected();
    }

    private void OnAbilityDeselected(int index)
    {
        _skillIcons[index].Deselected();
    }


    private void OnSkillAdded(Skill skill)
    {
        //UpdatePanel();
    }

    private void OnSkillRemoved(Skill skill)
    {
        //UpdatePanel();
    }

    public void UpdateKeys()
    {
        _skillIcons[0].Key.text = InputHandler.Instance.InputActions.GameplayMap.Spell1.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
        _skillIcons[1].Key.text = InputHandler.Instance.InputActions.GameplayMap.Spell2.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
        _skillIcons[2].Key.text = InputHandler.Instance.InputActions.GameplayMap.Spell3.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
        _skillIcons[3].Key.text = InputHandler.Instance.InputActions.GameplayMap.Spell4.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
        _skillIcons[4].Key.text = InputHandler.Instance.InputActions.GameplayMap.Spell5.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
        _skillIcons[5].Key.text = InputHandler.Instance.InputActions.GameplayMap.Spell6.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
        _skillIcons[6].Key.text = InputHandler.Instance.InputActions.GameplayMap.Spell7.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
        _skillIcons[7].Key.text = InputHandler.Instance.InputActions.GameplayMap.Spell8.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
        _skillIcons[8].Key.text = InputHandler.Instance.InputActions.GameplayMap.Spell9.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
        _skillIcons[9].Key.text = InputHandler.Instance.InputActions.GameplayMap.Spell10.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
        _skillIcons[10].Key.text = InputHandler.Instance.InputActions.GameplayMap.Spell11.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
        _skillIcons[11].Key.text = InputHandler.Instance.InputActions.GameplayMap.Spell12.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
        _skillIcons[12].Key.text = InputHandler.Instance.InputActions.GameplayMap.Spell13.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
        _skillIcons[13].Key.text = InputHandler.Instance.InputActions.GameplayMap.Spell14.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
        _skillIcons[14].Key.text = InputHandler.Instance.InputActions.GameplayMap.Spell15.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
        _skillIcons[15].Key.text = InputHandler.Instance.InputActions.GameplayMap.Spell16.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);
    }

    public void OnRebindSpellKeys(RebindUI rebindUI, string key, string deviceLayoutName, string controlPath)
    {
        UpdateKeys();
    }

    #region Debug

    private void SelectSkill(int arg0)
    {
        Debug.Log(arg0);
    }

    #endregion
}
