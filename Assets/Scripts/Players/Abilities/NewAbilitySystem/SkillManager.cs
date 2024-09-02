using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(SkillRenderer))]
[RequireComponent(typeof(SkillQueue))]
[RequireComponent(typeof(AutoAttackQueue))]
public class SkillManager : MonoBehaviour
{
    [SerializeField] private List<Skill> _skills;
    [SerializeField] private HeroComponent _hero;
    [SerializeField] private TalentSystem _talentSystem;

    private List<AutoAttackSkill> _autoAttackSkills = new List<AutoAttackSkill>();
    private List<Skill> _simpleSkills = new List<Skill>();
    private SkillRenderer _skillRenderer;
    private float _globalCooldownTime = 0.1f;
    private SkillQueue _skillQueue;
    private AutoAttackQueue _autoAttackQueue;
    private Skill _selectedSkill;

    public TalentSystem TalentSystem => _talentSystem;
    public List<Skill> Abilities => _skills;
    public SkillQueue SkillQueue { get => _skillQueue; }

    public event Action<int> SkillSelected;
    public event Action<int> SkillDeselected;
    public event Action<Skill> SkillAdded;
    public event Action<Skill> SkillRemoved;

    private void Awake()
    {
        _skillRenderer = GetComponent<SkillRenderer>();
        _skillQueue = GetComponent<SkillQueue>();
        _autoAttackQueue = GetComponent<AutoAttackQueue>();

        _hero.SelectComponent.IsSelected += OnSelected;

        foreach (var item in _skills)
        {
            AddSkill(item);
        }
    }

    public void AddSkill(Skill skill)
    {
        if(_skills.Contains(skill) == false)
            _skills.Add(skill);

        skill.Init(_skillRenderer, _hero);

        if (skill is AutoAttackSkill attackSkill)
        {
            _autoAttackSkills.Add(attackSkill);
        }
        else
        {
            _simpleSkills.Add(skill);
            skill.CastStarted += GlobalCooldown;
        }

        foreach (var simpleSkill in _simpleSkills)
        {
            foreach (var autoAttackSkill in _autoAttackSkills)
            {
                simpleSkill.CastStarted += autoAttackSkill.Pause;
                simpleSkill.CastEnded += autoAttackSkill.Continue;
            }
        }

        SkillAdded?.Invoke(skill);
    }

    public void RemoveSkill(Skill skill)
    {
        foreach (var simpleSkill in _simpleSkills)
        {
            foreach (var autoAttackSkill in _autoAttackSkills)
            {
                simpleSkill.CastStarted -= autoAttackSkill.Pause;
                simpleSkill.CastEnded -= autoAttackSkill.Continue;
            }
        }

        if (skill is AutoAttackSkill attackSkill)
        {
            _autoAttackSkills.Remove(attackSkill);
        }
        else
        {
            _simpleSkills.Remove(skill);
            skill.CastStarted -= GlobalCooldown;
        }
        _skills.Remove(skill);

        SkillRemoved?.Invoke(skill);
    }

    public void SetAbilitiesCoolDown(float time)
    {
        foreach (var item in _skills)
        {
            item.IncreaseSetCooldown(time);
        }
    }

    private void OnSelected(bool value)
    {
        if (value)
        {
            InputHandler.OnClick += PrepereSkill;
            InputHandler.OnAltClick += CancelSkillCast;

            InputHandler.OnFirstCast += SelectSkill;
            InputHandler.OnSecondCast += SelectSkill;
            InputHandler.OnThirdCast += SelectSkill;
            InputHandler.OnFourthCast += SelectSkill;
            InputHandler.OnFifthCast += SelectSkill;
            InputHandler.OnSixthCast += SelectSkill;
            InputHandler.OnSeventhCast += SelectSkill;
            InputHandler.OnEighthCast += SelectSkill;
        }
        else
        {
            InputHandler.OnClick -= PrepereSkill;
            InputHandler.OnAltClick -= CancelSkillCast;

            InputHandler.OnFirstCast -= SelectSkill;
            InputHandler.OnSecondCast -= SelectSkill;
            InputHandler.OnThirdCast -= SelectSkill;
            InputHandler.OnFourthCast -= SelectSkill;
            InputHandler.OnFifthCast -= SelectSkill;
            InputHandler.OnSixthCast -= SelectSkill;
            InputHandler.OnSeventhCast -= SelectSkill;
            InputHandler.OnEighthCast -= SelectSkill;
        }
    }

    public void SetAbilitiesCoolDown(float time)
    {
        foreach (var item in _skills)
        {
            item.IncreaseSetCooldown(time);
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
        else if(_selectedSkill != null)
        {
            SkillDeselected?.Invoke(_skills.IndexOf(_selectedSkill));
            UnsubscribingSkillOnEvents(_selectedSkill);
            _selectedSkill = null;
        }
    }

    private void SelectSkill(int index)
    {
        if (_selectedSkill != null && _selectedSkill.IsPreparing == true)
            return;

        if (_selectedSkill == _skills[index])
        {
            SkillSelected?.Invoke(index);

            PrepereSkill();
        }
        else if (_selectedSkill == null)
        {
            _selectedSkill = _skills[index];
            SubscribingSkillOnEvents(_selectedSkill);
            SkillSelected?.Invoke(index);

            PrepereSkill();
        }
        else if (_selectedSkill != _skills[index])
        {
            UnsubscribingSkillOnEvents(_selectedSkill);
            SkillDeselected?.Invoke(_skills.IndexOf(_selectedSkill));

            _selectedSkill = _skills[index];
            SubscribingSkillOnEvents(_selectedSkill);
            SkillSelected?.Invoke(index);

            PrepereSkill();
        }
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

    private void OnPreperingSuccess()
    {
        if(_selectedSkill is AutoAttackSkill attackSkill)
            _autoAttackQueue.Add(attackSkill);
        else
            SkillQueue.Add(_selectedSkill);
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
