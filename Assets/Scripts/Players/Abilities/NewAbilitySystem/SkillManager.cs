using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(SkillRenderer))]
public class SkillManager : MonoBehaviour
{
    [SerializeField] private List<Skill> _skills;
    [SerializeField] private HeroComponent _hero;
    [SerializeField] private TalentSystem _talentSystem;

    private List<AutoAttackSkill> _autoAttackSkills = new List<AutoAttackSkill>();
    private List<Skill> _simpleSkills = new List<Skill>();
    private SkillRenderer _skillRenderer;
    private float _globalCooldownTime = 2f;
    private SkillQueue _skillQueue;
    private AutoAttackQueue _autoAttackQueue;
    private Skill _selectedSkill;

    public TalentSystem TalentSystem => _talentSystem;

    public event Action<int> SkillSelected;
    public event Action<int> SkillDeselected;

    private void Awake()
    {
        _skillRenderer = GetComponent<SkillRenderer>();

        foreach (var item in _skills)
        {
            item.Init(_skillRenderer, _hero);

            if (item is AutoAttackSkill attackSkill)
            {
                _autoAttackSkills.Add(attackSkill);
            }
            else
            {
                _simpleSkills.Add(item);
            }
        }
        foreach (var simpleSkill in _simpleSkills)
        {
            foreach (var autoAttackSkill in _autoAttackSkills)
            {
                simpleSkill.CastStarted += autoAttackSkill.Pause;
                simpleSkill.CastEnded += autoAttackSkill.Continue;
            }
        }
    }

    private void OnEnable()
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

    private void OnDisable()
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
        else if (_skillQueue.IsBusy)
        {
            _skillQueue.TryCancel();
        }
        else if (_autoAttackQueue.IsBusy)
        {
            _autoAttackQueue.TryCancel();
        }
        else if (_skillQueue.IsEmpty == false)
        {
            _skillQueue.TryCancel();
        }
    }

    private void SelectSkill(int index)
    {
        if (_selectedSkill.IsPreparing == true) // ----
            return;

        if (_selectedSkill == null)
        {
            _selectedSkill = _skills[index];
            SubscribingSkillOnEvents(_selectedSkill);

            PrepereSkill();
        }
        else if (_selectedSkill == _skills[index])
        {
            PrepereSkill();
        }
        else if (_selectedSkill != _skills[index] && _selectedSkill != null)
        {
            UnsubscribingSkillOnEvents(_selectedSkill);

            _selectedSkill = _skills[index];
            SubscribingSkillOnEvents(_selectedSkill);

            PrepereSkill();
        }
    }

    private void SubscribingSkillOnEvents(Skill skill)
    {
        skill.PreparingSuccess += OnPreperingSuccess;
    }

    private void UnsubscribingSkillOnEvents(Skill skill)
    {
        skill.PreparingSuccess -= OnPreperingSuccess;
    }

    private void OnPreperingSuccess()
    {
        if(_selectedSkill is AutoAttackSkill attackSkill)
            _autoAttackQueue.Add(attackSkill);
        else
            _skillQueue.Add(_selectedSkill);
    }
}
