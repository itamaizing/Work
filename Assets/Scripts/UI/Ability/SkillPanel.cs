using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class SkillPanel : MonoBehaviour
{
    [SerializeField] private Camera _uiCamera;
    [SerializeField] private float _cameraCanvasDistance = 100;
    [SerializeField] private bool _hideUnusedButtons = true;
    [SerializeField] private List<RebindUI> _rebindsUI;
    [SerializeField] private SkillIcon[] _skillIcons;
    [SerializeField] private DraggableIcon _draggableIconPref;
    [SerializeField] private FillAmountOverTime _castLine;
    [SerializeField] private QueuePanel _queuePanel;
    [SerializeField] private AbilityNameBox _abilityNameBox;

    private List<DraggableIcon> _skills = new List<DraggableIcon>();
    private Character _currentCharacter;
    private SkillManager _playerAbilities;
    private bool _isActive;
    private bool _isSelect;
    private bool _isMenu = false;
    private SaveSystem _saveSystem = new();

    private void Start()
    {
        UpdateKeys();

        foreach (var item in _rebindsUI)
        {
            item.updateBindingUIEvent.AddListener(OnRebindSpellKeys);
        }

        for (int i = 0; i < _skillIcons.Length; i++)
        {
            _skillIcons[i].Init(i);
            _skillIcons[i].CurrentSkillChenged += SkillChenged;
        }
    }

    public void Fill(SkillManager abilities)
    {
        Debug.Log("Refill");
        ClearPanel();

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
            if (_playerAbilities.SelectedSkills[i] == null)
            {
                _skillIcons[i].CurrentIcon = null;
                continue;
            }
                

            var icon = Instantiate(_draggableIconPref, _skillIcons[i].transform);
            icon.Init(_playerAbilities.SelectedSkills[i], _skillIcons[i].transform, _uiCamera, _cameraCanvasDistance);
            _skillIcons[i].CurrentIcon = icon;
            icon.transform.SetAsFirstSibling();
            _skills.Add(icon);

            icon.BeginDrag += OnBeginDrag;
            icon.EndDrag += OnEndDrag;
            icon.PointerEnter += OnPointerEnterIcon;
            icon.PointerExit += OnPointerExitIcon;
        }

        _playerAbilities.SkillSelected += OnAbilitySelected;
        _playerAbilities.SkillDeselected += OnAbilityDeselected;
        _playerAbilities.SkillAdded += OnSkillAdded;
        _playerAbilities.SkillRemoved += OnSkillRemoved;

        OnBeginDrag();
        LoadPanel();
        OnEndDrag();
    }

    public void FillMenu(SkillManager abilities)
    {
        _isMenu = true;
        ClearPanel();

        if (_playerAbilities != null)
        {
            _playerAbilities.SkillAdded -= OnSkillAdded;
            _playerAbilities.SkillRemoved -= OnSkillRemoved;
        }

        _playerAbilities = abilities;

        for (int i = 0; i < _playerAbilities.Skills.Count; i++)
        {
            if (_playerAbilities.Skills[i] == null)
            {
                _skillIcons[i].CurrentIcon = null;
                continue;
            }
            if (i >= _skillIcons.Length) return;

            var icon = Instantiate(_draggableIconPref, _skillIcons[i].transform);
            icon.Init(_playerAbilities.Skills[i], _skillIcons[i].transform, _uiCamera, _cameraCanvasDistance, true);
            _skillIcons[i].CurrentIcon = icon;
            icon.transform.SetAsFirstSibling();
            _skills.Add(icon);

            icon.BeginDrag += OnBeginDrag;
            icon.EndDrag += OnEndDrag;
            icon.PointerEnter += OnPointerEnterIcon;
            icon.PointerExit += OnPointerExitIcon;
        }

        _playerAbilities.SkillAdded += OnSkillAdded;
        _playerAbilities.SkillRemoved += OnSkillRemoved;

        OnBeginDrag();
        LoadPanel();

        OnEndDrag();
    }

    public void FillMinionPanel(SkillManager abilities)
    {
        if (_playerAbilities != null)
        {
            _playerAbilities.SkillSelected -= OnAbilitySelected;
            _playerAbilities.SkillDeselected -= OnAbilityDeselected;
            _playerAbilities.SkillAdded -= OnSkillAdded;
            _playerAbilities.SkillRemoved -= OnSkillRemoved;
        }

        _playerAbilities = abilities;

        for (int i = 0; i < abilities.SelectedSkills.Length; i++)
        {
            var skill = abilities.SelectedSkills[i];
            if (skill == null) continue;

            var freeIcon = _skillIcons.FirstOrDefault(icon => icon.CurrentIcon == null);
            if (freeIcon == null) break;

            var icon = Instantiate(_draggableIconPref, freeIcon.transform);
            icon.Init(skill, freeIcon.transform, _uiCamera, _cameraCanvasDistance);
            freeIcon.CurrentIcon = icon;
            freeIcon.Show();
            icon.transform.SetAsFirstSibling();
            _skills.Add(icon);

            icon.BeginDrag += OnBeginDrag;
            icon.EndDrag += OnEndDrag;
            icon.PointerEnter += OnPointerEnterIcon;
            icon.PointerExit += OnPointerExitIcon;
        }

        _playerAbilities.SkillSelected += OnAbilitySelected;
        _playerAbilities.SkillDeselected += OnAbilityDeselected;
        _playerAbilities.SkillAdded += OnSkillAdded;
        _playerAbilities.SkillRemoved += OnSkillRemoved;

        OnBeginDrag();
        OnEndDrag();
    }

    public void OnMinionSelected(SkillManager minionSkillManager)
    {
        FillMinionPanel(minionSkillManager);
    }

    public void SetHideUnusedButtons(bool value)
    {
        if (value)
        {
            _hideUnusedButtons = value;

            OnEndDrag();
        }
        else
        {
            _hideUnusedButtons = value;

            foreach (var item in _skillIcons)
            {
                item.Show();
            }
        }
    }

    private void OnPointerEnterIcon(DraggableIcon skill)
    {
        _abilityNameBox.Show(skill.Skill);
        _abilityNameBox.gameObject.SetActive(true);
    }

    private void OnPointerExitIcon(DraggableIcon skill)
    {
        _abilityNameBox.gameObject.SetActive(false);
    }

    private void OnBeginDrag()
    {
        if (_hideUnusedButtons)
        {
            foreach (var item in _skillIcons)
            {
                item.Show();
            }
        }
    }
    
    private void OnEndDrag()
    {
        SavePanel();
        if (_hideUnusedButtons)
        {
            foreach (var item in _skillIcons)
            {
                if (item.CurrentIcon == null)
                {
                    item.Hide();
                }
            }
        }
    }

    private void SkillChenged(int index, Skill skill)
    {
        if (_playerAbilities?.SelectedSkills == null) return;
        if (index < 0 || index >= _playerAbilities.SelectedSkills.Length) return;

        _playerAbilities.SelectedSkills[index] = skill;
    }

    public void OnCharacterSelected(Character character)
    {
        if (character != null && character != _currentCharacter)
        {
            gameObject.SetActive(true);
            _currentCharacter = character;
            if(_isMenu)
                FillMenu(_currentCharacter.Abilities);
            else
                Fill(_currentCharacter.Abilities);
            _queuePanel.Init(character.Abilities.SkillQueue);
        }
    }

    public void OnCharacterDeselected(Character character)
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
        Debug.Log("Clear");
        foreach (var item in _skills)
        {
            Destroy(item.gameObject);
        }
        _skills.Clear();
        foreach(var ico in _skillIcons)
        {
            ico.CurrentIcon = null;
        }
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
        if (_isMenu)
            FillMenu(_playerAbilities);
        else
            Fill(_playerAbilities);
		//UpdatePanel();
	}

    private void OnSkillRemoved(Skill skill)
    {
        if (_isMenu)
            FillMenu(_playerAbilities);
        else
            Fill(_playerAbilities);
        //UpdatePanel();
    }

    public bool HasSkill(Skill skill)
    {
        if (skill == null) return false;
        return _skills.Any(icon => icon.Skill != null && icon.Skill.GetType() == skill.GetType());
    }

    public void AddSkill(Skill skill)
    {
        if (skill == null) return;

        if (_skills.Any(icon => icon.Skill == skill)) return;

        var freeIcon = _skillIcons.FirstOrDefault(icon => icon.CurrentIcon == null);
        if (freeIcon == null) return;

        var icon = Instantiate(_draggableIconPref, freeIcon.transform);
        icon.Init(skill, freeIcon.transform, _uiCamera, _cameraCanvasDistance);
        freeIcon.CurrentIcon = icon;
        freeIcon.Show();
        icon.transform.SetAsFirstSibling();
        _skills.Add(icon);

        LoadOneSkill(skill);

        icon.BeginDrag += OnBeginDrag;
        icon.EndDrag += OnEndDrag;
        icon.PointerEnter += OnPointerEnterIcon;
        icon.PointerExit += OnPointerExitIcon;
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

    private void SavePanel()
    {
        List<SkillPanelSave> save = new();
        for(int i = 0; i< _skillIcons.Length; i++)
        {
            if (_skillIcons[i].CurrentIcon != null)
            {
                SkillPanelSave item = new();
                item.Name = _skillIcons[i].CurrentIcon.Skill.Name;
                item.Id = i;

                save.Add(item);
                //Debug.Log("Ico  " + i + " Ability" + _skillIcons[i].CurrentIcon.Skill.Name);
            }
        }
        _saveSystem.Save($"{_playerAbilities.Hero.Data.Name}_Group{0}_AbilityPanel", save);

        
    }

    private void LoadPanel()
    {
        List<SkillPanelSave> save = new();
        _saveSystem.Load<List<SkillPanelSave>>($"{_playerAbilities.Hero.Data.Name}_Group{0}_AbilityPanel", e => save = e);
        if (save == null) return;
        Debug.Log("Save loaded");

        foreach(var skillSave in save)
        {
            DraggableIcon icon = _skills.FirstOrDefault(a => a.Skill.Name == skillSave.Name);
            SkillIcon cell = _skillIcons.FirstOrDefault(a => a.CurrentIcon == icon);
            if (icon == null || cell == null) continue;
            Debug.Log("Save inited");
            cell.CurrentIcon = null;

            if (_skillIcons[skillSave.Id].CurrentIcon != null)
            {
                DraggableIcon iconTemp = _skillIcons[skillSave.Id].CurrentIcon;
                SkillIcon cellTemp = _skillIcons.FirstOrDefault(a => a.CurrentIcon == null);
                cellTemp.CurrentIcon = iconTemp;
                iconTemp.UpdatePosition(cellTemp.transform);
                _skillIcons[skillSave.Id].CurrentIcon = null;
            }
            _skillIcons[skillSave.Id].CurrentIcon = icon;
            icon.UpdatePosition(_skillIcons[skillSave.Id].transform);
        }
        Debug.Log("Load");
    }

    private void LoadOneSkill(Skill skill)
    {
        List<SkillPanelSave> save = new();
        _saveSystem.Load<List<SkillPanelSave>>($"{_playerAbilities.Hero.Data.Name}_Group{0}_AbilityPanel", e => save = e);
        if (save == null) return;
        Debug.Log("Save loaded");

        DraggableIcon icon = _skills.FirstOrDefault(a => a.Skill.Name == skill.Name);
        SkillIcon cell = _skillIcons.FirstOrDefault(a => a.CurrentIcon == icon);
        SkillPanelSave saveItem = save.FirstOrDefault(a => a.Name == skill.Name);

        if (icon == null || cell == null) return;
        Debug.Log("Save inited");
        cell.CurrentIcon = null;

        if (_skillIcons[saveItem.Id].CurrentIcon != null)
        {
            DraggableIcon iconTemp = _skillIcons[saveItem.Id].CurrentIcon;
            SkillIcon cellTemp = _skillIcons.FirstOrDefault(a => a.CurrentIcon == null);
            cellTemp.CurrentIcon = iconTemp;
            iconTemp.UpdatePosition(cellTemp.transform);
            _skillIcons[saveItem.Id].CurrentIcon = null;
        }
        _skillIcons[saveItem.Id].CurrentIcon = icon;
        icon.UpdatePosition(_skillIcons[saveItem.Id].transform);
        Debug.Log("Load");
    }
}

public struct SkillPanelSave
{
    public string Name;
    public int Id;
}
