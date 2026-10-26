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

    public void Init(SkillQueue queue)
    {
        Unsubscribe();
        _skillQueue = queue;

        _skillQueue.SkillAdded += OnSkillAdded;
        _skillQueue.SkillDeleted += OnSkillDeleted;
    }

    private void OnDestroy() => Unsubscribe();

    private void Unsubscribe()
    {
        if (_skillQueue == null) return;
        _skillQueue.SkillAdded -= OnSkillAdded;
        _skillQueue.SkillDeleted -= OnSkillDeleted;
    }

    private void OnSkillAdded(Skill skill)
    {
        var icon = Instantiate(_iconPref, transform);
        icon.Init(skill);
        ShowTarget(icon, skill);

        _entries.Add(new Entry { Skill = skill, Icon = icon });
    }
    
    private void OnSkillDeleted(Skill skill)
    {
        int index = _entries.FindIndex(e => e.Skill == skill);
        if (index < 0) return;

        var icon = _entries[index].Icon;
        _entries.RemoveAt(index);
        if (icon != null) Destroy(icon.gameObject);
    }

    private void ShowTarget(AbilityIcon icon, Skill skill)
    {
        TargetInfo info = skill.TargetInfoQueue.Count > 0 ? skill.TargetInfoQueue.Last() : null;

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