using System;
using System.Collections.Generic;
using System.Linq;

public class MinionSkillGroup
{
    public Type SkillType;
    public readonly List<Skill> Instances = new();
    public Skill Representative => Instances.FirstOrDefault();
}

public class MinionSkillPanelController
{
    private readonly SkillPanel _panel;
    private readonly Dictionary<Type, MinionSkillGroup> _groups = new();
    private readonly Dictionary<Character, List<Skill>> _byMinion = new();

    private readonly Dictionary<Skill, int> _skillRefCount = new();
    public MinionSkillPanelController(SkillPanel panel) => _panel = panel;

    public void RegisterMinion(Character minion)
    {
        if (minion == null || minion.IsDead) return;
        var skillManager = minion.GetComponent<SkillManager>();
        if (skillManager == null) return;

        var ownSkills = new List<Skill>();

        foreach (var skill in skillManager.SelectedSkills)
        {
            if (skill == null) continue;

            ownSkills.Add(skill);

            _skillRefCount.TryGetValue(skill, out var count);
            _skillRefCount[skill] = count + 1;

            if (count > 0)
                continue;

            var type = skill.GetType();
            if (!_groups.TryGetValue(type, out var group))
            {
                group = new MinionSkillGroup { SkillType = type };
                _groups[type] = group;
            }

            if (group.Instances.Contains(skill))
                continue;
            
            bool isNewGroup = group.Instances.Count == 0;
            group.Instances.Add(skill);

            if (isNewGroup)
                AddGroupIcon(group);
        }

        _byMinion[minion] = ownSkills;
    }

    public void UnregisterMinion(Character minion)
    {
        if (minion == null || !_byMinion.TryGetValue(minion, out var ownSkills))
            return;

        foreach (var skill in ownSkills)
        {
            if (skill == null) continue;

            if (!_skillRefCount.TryGetValue(skill, out var count))
                continue;

            count--;
            if (count > 0)
            {
                _skillRefCount[skill] = count;
                continue;
            }

            _skillRefCount.Remove(skill);

            var type = skill.GetType();
            if (!_groups.TryGetValue(type, out var group)) continue;

            bool wasRepresentative = group.Representative == skill;
            group.Instances.Remove(skill);

            if (group.Instances.Count == 0)
            {
                _panel.RemoveSkill(skill);
                _groups.Remove(type);
            }
            else if (wasRepresentative)
            {
                _panel.GetIcon(skill)?.Rebind(group.Representative);
            }
        }

        _byMinion.Remove(minion);
    }

    public void Clear()
    {
        foreach (var group in _groups.Values)
            _panel.RemoveSkill(group.Representative);

        _groups.Clear();
        _byMinion.Clear();
        _skillRefCount.Clear();
    }

    private void AddGroupIcon(MinionSkillGroup group)
    {
        _panel.AddSkill(group.Representative);
        var icon = _panel.GetIcon(group.Representative);
        if (icon != null)
            icon.ClickOverride = _ => { IssueGroupOrder(group); return true; };
    }

    private void IssueGroupOrder(MinionSkillGroup group)
    { 
        foreach (var skill in group.Instances.ToList())
        {
            if (skill == null || skill.Hero == null || skill.Hero.IsDead) continue;
            skill.Hero.GetComponent<SkillManager>()?.SelectAndPrepareSkill(skill);
        }
    }
}