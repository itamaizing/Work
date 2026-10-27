using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class QueuePanel : MonoBehaviour
{
    [SerializeField] private AbilityIcon _iconPref;

    [Header("Target icons")]
    [SerializeField] private Sprite _areaIcon;
    [SerializeField] private Sprite _projectileIcon;
    [SerializeField] private Sprite _unknownTargetIcon;

    private sealed class Entry
    {
        public Skill Skill;
        public AbilityIcon Icon;
    }

    private readonly List<Entry> _entries = new();
    private SkillQueue _skillQueue;
    private AutoSkillCast _autoCast;

    private AbilityIcon _autoIcon;
    private Skill _autoSkill;

    public void Init(SkillQueue queue, AutoSkillCast autoCast)
    {
        Unsubscribe();
        ClearAll();

        _skillQueue = queue;
        _autoCast = autoCast;

        _skillQueue.SkillAdded += OnSkillAdded;
        _skillQueue.SkillDeleted += OnSkillDeleted;
        
        var occurrences = new Dictionary<Skill, int>();
        foreach (var skill in _skillQueue.Skills)
        {
            occurrences.TryGetValue(skill, out int n);
            occurrences[skill] = n + 1;
            AddEntry(skill, FindQueuedInfo(skill, n));
        }

        if (_autoCast != null)
        {
            _autoCast.SkillSet += OnAutoSkillSet;
            _autoCast.SkillCleared += OnAutoSkillCleared;
            if (_autoCast.IsBusy) SetAutoSkill(_autoCast.CurrentSkill, _autoCast.CurrentTargetInfo);
        }
    }

    private void OnAutoSkillSet(Skill skill, TargetInfo info) => SetAutoSkill(skill, info);
    private void OnAutoSkillCleared() => ClearAutoSkill();

    private void ClearAll()
    {
        foreach (var entry in _entries)
            if (entry.Icon != null) Destroy(entry.Icon.gameObject);
        _entries.Clear();
        ClearAutoSkill();
    }

    private static TargetInfo FindQueuedInfo(Skill skill, int occurrence)
    {
        var infos = skill.TargetInfoQueue.ToArray();
        return occurrence < infos.Length ? infos[occurrence] : null;
    }

    private void OnDestroy()
    {
        Unsubscribe();
        ClearAutoSkill();
    }

    private void Unsubscribe()
    {
        if (_skillQueue == null) return;
        _skillQueue.SkillAdded -= OnSkillAdded;
        _skillQueue.SkillDeleted -= OnSkillDeleted;
        _skillQueue = null;

        if (_autoCast != null)
        {
            _autoCast.SkillSet -= OnAutoSkillSet;
            _autoCast.SkillCleared -= OnAutoSkillCleared;
            _autoCast = null;
        }
    }

    private void OnSkillAdded(Skill skill)
    {
        TargetInfo info = skill.TargetInfoQueue.Count > 0 ? skill.TargetInfoQueue.Last() : null;
        AddEntry(skill, info);
    }

    private void AddEntry(Skill skill, TargetInfo info)
    {
        var icon = Instantiate(_iconPref, transform);
        icon.Init(skill);
        ShowTarget(icon, skill, info);

        _entries.Add(new Entry { Skill = skill, Icon = icon });
    }
    
    public void SetAutoSkill(Skill skill, TargetInfo info)
    {
        if (skill == null)
        {
            ClearAutoSkill();
            return;
        }

        if (_autoIcon == null || _autoSkill != skill)
        {
            ClearAutoSkill();
            _autoIcon = Instantiate(_iconPref, transform);
            _autoIcon.Init(skill);
            _autoSkill = skill;
        }

        _autoIcon.transform.SetAsFirstSibling();
        _autoIcon.SetAutoCast(true);
        ShowTarget(_autoIcon, skill, info);
    }

    public void ClearAutoSkill()
    {
        if (_autoIcon != null) Destroy(_autoIcon.gameObject);
        _autoIcon = null;
        _autoSkill = null;
    }

    private void OnSkillDeleted(Skill skill)
    {
        int index = _entries.FindIndex(e => e.Skill == skill);
        if (index < 0) return;

        var icon = _entries[index].Icon;
        _entries.RemoveAt(index);
        if (icon != null) Destroy(icon.gameObject);
    }

    private void ShowTarget(AbilityIcon icon, Skill skill, TargetInfo info)
    {
        Character firstCharacter = null;
        string fallbackName = null;
        int count = 0;

        if (info != null)
        {
            foreach (var target in info.GetTargets())
            {
                if (target == null) continue;

                count++;
                if (firstCharacter == null && target is Character character) firstCharacter = character;
                if (fallbackName == null && target is Component component) fallbackName = component.name;
            }
        }

        if (count > 0)
        {
            string label = firstCharacter != null ? GetTargetName(firstCharacter) : fallbackName;
            if (count > 1) label += $" +{count - 1}";

            icon.ShowTarget(firstCharacter != null ? GetTargetSprite(firstCharacter) : _unknownTargetIcon, label);
            return;
        }

        bool hasPoints = info != null && info.Points != null && info.Points.Count > 0;
        if (!hasPoints)
        {
            icon.HideTarget();
            return;
        }

        bool isArea = skill.Targeting.SkillType == SkillType.Zone;
        icon.ShowTarget(isArea ? _areaIcon : _projectileIcon, null);
    }

    private static string GetTargetName(Character character)
    {
        return character.Data != null ? character.Data.Name : character.name;
    }

    private Sprite GetTargetSprite(Character character)
    {
        Sprite icon = character.Data != null ? character.Data.Icon : null;
        return icon != null ? icon : _unknownTargetIcon;
    }
}