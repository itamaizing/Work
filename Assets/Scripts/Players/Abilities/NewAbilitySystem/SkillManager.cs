using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(SkillQueue))]
[RequireComponent(typeof(AutoSkillQueue))]
[RequireComponent(typeof(AutoAttackQueue))]
public class SkillManager : MonoBehaviour
{
    [SerializeField] private List<Skill> _skills;
    [SerializeField] private Character _hero;
    [SerializeField] private TalentSystem _talentSystem;
    [SerializeField] private SkillRenderer _skillRenderer;

    private Skill[] _selectedSkills = new Skill[16];
    private List<AutoAttackSkill> _autoAttackSkills = new List<AutoAttackSkill>();
    private List<AutoSkill> _autoSkills = new List<AutoSkill>();
    private List<Skill> _simpleSkills = new List<Skill>();
    private float _globalCooldownTime = .5f;
    private SkillQueue _skillQueue;
    private AutoSkillQueue _autoSkillQueue;
    private AutoAttackQueue _autoAttackQueue;
    private Skill _selectedSkill;

    public TalentSystem TalesntSystem => _talentSystem;
    public SkillQueue SkillQueue { get => _skillQueue; }
    public Skill[] SelectedSkills { get => _selectedSkills; }
    public IEnumerable<Skill> DefaultSkills => _skills.Where(o => o.IsTalentSpell == false);
    public IEnumerable<Skill> TalentsSkills => _skills.Where(o => o.IsTalentSpell);

    public List<Skill> Abilities => _skills;
    public event Action<int> SkillSelected;
    public event Action<int> SkillDeselected;
    public event Action<Skill> SkillAdded;
    public event Action<Skill> SkillRemoved;

    private void Awake()
    {
        InputHandler.ScrollMouse += ScrollMouse;

        _skillQueue = GetComponent<SkillQueue>();
        _autoAttackQueue = GetComponent<AutoAttackQueue>();
        _autoSkillQueue = GetComponent<AutoSkillQueue>();

        _autoSkillQueue.SkillActivated += AutoSkillUsed;

        foreach (var item in _skills)
        {
            AddToSkillLists(item);
            SkillInit(item);
        }
    }

    public void CancleAllSkills()
    {
        while(_selectedSkill != null && _selectedSkill.IsPreparing)
        {
            CancelSkillCast();
        }
        while (SkillQueue.IsBusy)
        {
            CancelSkillCast();
        }
        while (_autoAttackQueue.IsBusy)
        {
            CancelSkillCast();
        }
        while (SkillQueue.IsEmpty == false)
        {
            CancelSkillCast();
        }
    }

	private void ScrollMouse(float value)
	{
        if (_selectedSkill == null) return;
        
		var index = Array.IndexOf(_selectedSkills, _selectedSkill);

		if (value > 0)
        {            
            if(index - 1 < 0)
            {
                index = _skills.Count;
			}
			SelectSkill(index - 1);
		}
        if(value < 0)
        {
			if (index >= _skills.Count)
			{
                index = 0;
			}
			SelectSkill(index + 1);
		}
       
	}

    private void AddToSkillLists(Skill skill)
    {
        if (skill is AutoAttackSkill attackSkill)
        {
            _autoAttackSkills.Add(attackSkill);
        }
        else if (skill is AutoSkill autoSkill)
        {
            _autoSkills.Add(autoSkill);
            skill.CastStarted += GlobalCooldown;
        }
        else
        {
            _simpleSkills.Add(skill);
            skill.CastStarted += GlobalCooldown;
        }
    }

    private void SkillInit(Skill skill)
    {
        skill.Init(_skillRenderer, _hero);

        foreach (var simpleSkill in _simpleSkills)
        {
            foreach (var autoAttackSkill in _autoAttackSkills)
            {
                simpleSkill.CastStarted += autoAttackSkill.Pause;
                simpleSkill.CastEnded += autoAttackSkill.Continue;
            }
        }
        
        ToggleSkillActivation(skill);
    }

    public void ActivateSkill(Skill skill)
    {
        for (int i = 0; i < _selectedSkills.Length; i++)
        {
            if (_selectedSkills[i] == null && !_selectedSkills.Contains(skill))
            {
                _selectedSkills[i] = skill;
                break;
            }
        }
        
        skill.IsSkillActive = true;
        SkillAdded?.Invoke(skill);
    }

    public void DeactivateSkill(Skill skill)
    {
        for (int i = 0; i < _selectedSkills.Length; i++)
        {
            if (_selectedSkills[i] == skill && _selectedSkills.Contains(skill))
            {
                _selectedSkills[i] = null;
                break;
            }
        }
        
        skill.IsSkillActive = false;
        SkillRemoved?.Invoke(skill);
    }

    private void ToggleSkillActivation(Skill skill)
    {
        if (skill.IsSkillActive)
        {
            ActivateSkill(skill);
        }
        else
        {
            DeactivateSkill(skill);
        }
    }

    public void SetAbilitiesCoolDown(float time)
    {
        foreach (var item in _skills)
        {
            item.IncreaseSetCooldown(time);
        }
    }

    public void OnSelect(bool value)
    {
        if (value)
        {
            InputHandler.OnClick += PrepereSkill;
            InputHandler.OnAltClick += CancelSkillCast;

            InputHandler.OnCast += OnCastSelect;
        }
        else
        {
            InputHandler.OnClick -= PrepereSkill;
            InputHandler.OnAltClick -= CancelSkillCast;

            InputHandler.OnCast -= OnCastSelect;

            if (_selectedSkill != null && _selectedSkill.IsPreparing)
            {
                _selectedSkill.TryCancel();

                DeselectSkill();
            }
        }
    }

    private void PrepereSkill()
    {
        if(_selectedSkill != null)
        {
            _selectedSkill.TryPreparing();
        }
    }

    private void CancelSkillCast()
    {
        if (_selectedSkill != null && _selectedSkill.IsPreparing)
        {
            _selectedSkill.TryCancel();

            if(_selectedSkill is AutoAttackSkill aa)
                DeselectSkill();
        }
        else if (SkillQueue.IsBusy)
        {
            SkillQueue.TryCancel();
        }
        else if (_autoAttackQueue.IsBusy)
        {
            _autoAttackQueue.TryCancel();
        }
        else if (SkillQueue.IsEmpty == false)
        {
            SkillQueue.TryCancel();
        }
        /*else if(_selectedSkill != null)
        {
            DeselectSkill();
        }*/ // not need now, but not deleted
    }

    private void OnCastSelect(int index)
    {
        if (SelectSkill(index))
        {
            PrepereSkill();
        }
	}

    private bool SelectSkill(int index)
    {
        if (_selectedSkills[index] == null)
            return false;

        if (_selectedSkill != null && _selectedSkill.IsPreparing == true)
        {
            if (_selectedSkill != _selectedSkills[index])
            {
                _selectedSkill.TryCancel(true);

                DeselectSkill();
                SetSelectSkill(_selectedSkills[index]);
                PrepereSkill();
            }
            return false;
        }

        if (_selectedSkill == _selectedSkills[index])
        {
            SkillSelected?.Invoke(index);
        }
        else if (_selectedSkill == null)
        {
            SetSelectSkill(_selectedSkills[index]);

        }
        else if (_selectedSkill != _selectedSkills[index])
        {
            DeselectSkill();
            SetSelectSkill(_selectedSkills[index]);
        }
        return true;
    }

    private void SetSelectSkill(Skill skill)
    {
        _selectedSkill = skill;
        SubscribingSkillOnEvents(_selectedSkill);
        SkillSelected?.Invoke(Array.IndexOf(_selectedSkills, skill));
    }

    private void DeselectSkill()
    {
        int index = Array.IndexOf(_selectedSkills, _selectedSkill);

        if (index == -1)
            return;

        SkillDeselected?.Invoke(index);
        UnsubscribingSkillOnEvents(_selectedSkill);
        _selectedSkill = null;
    }

    private void GlobalCooldown()
    {
        foreach (var item in _skills)
        {
            if(item.IsSubjectToGlobalCooldownTime)
                item.IncreaseSetCooldown(_globalCooldownTime);
        }
    }

    private void SubscribingSkillOnEvents(Skill skill)
    {
        skill.PreparingSuccess += OnPreperingSuccess;
    }

    private void UnsubscribingSkillOnEvents(Skill skill)
    {
        if (skill == null)
            return;

        skill.PreparingSuccess -= OnPreperingSuccess;
    }

    private void OnPreperingSuccess(Skill skill)
    {
        if(_selectedSkill is AutoAttackSkill attackSkill)
        {
            _autoAttackQueue.Add(attackSkill);

            DeselectSkill();
        }
        else
        {
            SkillQueue.Add(_selectedSkill);
        }     
    }

    private void AutoSkillUsed(Skill skill)
    {
        foreach (var item in _skills)
        {
            if(item != skill)
            {
                item.TryCancel(true);
            }
        }
    }

    #region legacycode
    private void OnDestroy()
    {
        /*
        AbilitiesManager.Instance.RemovePanel(_abilityPanel);
        */
    }
    public void AddAbility(Ability ability)
    {
        /*
        _abilities.Add(ability);
        if (AbilitiesManager.Instance == null) return;

        AbilitiesManager.Instance.RemovePanel(_abilityPanel);
        _abilityPanel = AbilitiesManager.Instance.AddPanel(this);
        _abilityPanel.gameObject.SetActive(true);
        */
    }
    public void RemoveAbility(Ability ability)
    {
        /*
        _abilities.Remove(ability);
        if (AbilitiesManager.Instance == null) return;

        AbilitiesManager.Instance.RemovePanel(_abilityPanel);
        _abilityPanel = AbilitiesManager.Instance.AddPanel(this);
        _abilityPanel.gameObject.SetActive(true);
        */
    }
    public void SetAbilitiesPanelSelect(bool isSelect)
    {
        /*
        AbilitiesManager.Instance.ChangeCurrentPanelSelectStatus(_abilityPanel, isSelect);
        if (isSelect) EnableAbilities();
        else DisableAbilities();
        */
    }
    public void SetAbilitiesPanelEnable()
    {
        /*
        AbilitiesManager.Instance.ActiveCurrentPanel(_abilityPanel);
        */
    }
    public void SetAbilitiesDisabled()
    {
        //_isAbilitiesDisabled = true;
    }
    public void SetAbilitiesEnabled()
    {
        //_isAbilitiesDisabled = false;
    }
    public void SwitchAvaliable(Schools school, bool value)
    {
        /*
        if (school == Schools.Physical)
            return;
        foreach (var item in _abilities)
        {
            if (item.School == school)
            {
                item.SwitchAvailible(value);
                //item.KnockDownTimerStart(coolDown);
            }
        }
        */
    }

    public void SwitchAvaliable(AbilityForm form, bool value)
    {
        /*
        foreach (var item in _abilities)
        {
            if (item.AbilityForm == form)
            {
                item.SwitchAvailible(value);
                //item.KnockDownTimerStart(coolDown);
            }
        }
        */
    }
    #endregion
}
