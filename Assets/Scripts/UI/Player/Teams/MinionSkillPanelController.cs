using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MinionSkillGroup
{
    public object GroupKey;
    public readonly List<Skill> Instances = new();
    public Skill Representative => Instances.FirstOrDefault();
}

public class MinionSkillPanelController
{
    private readonly SkillPanel _panel;
    private readonly Dictionary<object, MinionSkillGroup> _groups = new();
    private readonly Dictionary<Character, List<Skill>> _byMinion = new();

    private readonly Dictionary<Skill, int> _skillRefCount = new();
    private readonly Dictionary<Skill, object> _skillGroupKey = new();
    private readonly Dictionary<CreatureSpawn, Action<SpawnType>> _spawnTypeHandlers = new();

    public MinionSkillPanelController(SkillPanel panel)
    {
        _panel = panel;
        InputHandler.OnAltClick += CancelPreparingMinionSkills;
    }

    public void RegisterMinion(Character minion)
    {
        if (minion == null || minion.IsDead) return;
        var skillManager = minion.GetComponent<SkillManager>();
        if (skillManager == null) return;

        PruneDestroyed();
        
        var ownSkills = new List<Skill>();

        foreach (var skill in skillManager.SelectedSkills)
        {
            if (skill == null) continue;

            ownSkills.Add(skill);

            _skillRefCount.TryGetValue(skill, out var count);
            _skillRefCount[skill] = count + 1;

            if (skill is CreatureSpawn creatureSpawn && !_spawnTypeHandlers.ContainsKey(creatureSpawn))
            {
                Action<SpawnType> handler = _ => RegroupSkill(creatureSpawn);
                _spawnTypeHandlers[creatureSpawn] = handler;
                creatureSpawn.OnSpawnTypeChanged += handler;
            }

            if (count > 0)
                continue;

            InsertIntoGroup(skill, skill.GroupKey);
        }

        _byMinion[minion] = ownSkills;
    }

    private void InsertIntoGroup(Skill skill, object key)
    {
        if (!_groups.TryGetValue(key, out var group))
        {
            group = new MinionSkillGroup { GroupKey = key };
            _groups[key] = group;
        }

        if (group.Instances.Contains(skill))
        {
            return;
        }

        bool isNewGroup = group.Instances.Count == 0;
        group.Instances.Add(skill);
        _skillGroupKey[skill] = key;

        if (isNewGroup)
            AddGroupIcon(group);
    }

    private void RegroupSkill(Skill skill)
    {
        if (!_skillGroupKey.TryGetValue(skill, out var oldKey))
        {
            return;
        }

        var newKey = skill.GroupKey;

        if (Equals(oldKey, newKey))
        {
            return;
        }

        if (_groups.TryGetValue(oldKey, out var oldGroup))
        {
            bool wasRepresentative = oldGroup.Representative == skill;
            oldGroup.Instances.Remove(skill);

            if (oldGroup.Instances.Count == 0)
            {
                _panel.RemoveSkill(skill);
                _groups.Remove(oldKey);
            }
            else if (wasRepresentative)
            {
                _panel.GetIcon(skill)?.Rebind(oldGroup.Representative);
            }
        }

        InsertIntoGroup(skill, newKey);
    }


    public void UnregisterMinion(Character minion)
    {
        if (ReferenceEquals(minion, null) || !_byMinion.TryGetValue(minion, out var ownSkills))
            return;

        foreach (var skill in ownSkills)
        {
            if (!_skillRefCount.TryGetValue(skill, out var count))
                continue;

            count--;
            if (count > 0)
            {
                _skillRefCount[skill] = count;
                continue;
            }

            _skillRefCount.Remove(skill);

            if (!_skillGroupKey.TryGetValue(skill, out var key)) continue;
            if (!_groups.TryGetValue(key, out var group)) continue;

            bool wasRepresentative = group.Representative == skill;
            group.Instances.Remove(skill);
            _skillGroupKey.Remove(skill);
            
            if (skill is CreatureSpawn cs && _spawnTypeHandlers.TryGetValue(cs, out var handler))
            {
                cs.OnSpawnTypeChanged -= handler;
                _spawnTypeHandlers.Remove(cs);
            }

            if (group.Instances.Count == 0)
            {
                _panel.RemoveSkill(skill);
                _groups.Remove(key);
            }
            else if (wasRepresentative)
            {
                _panel.GetIcon(skill)?.Rebind(group.Representative);
            }
        }

        _byMinion.Remove(minion);
    }
    
    public void Dispose()
    {
        InputHandler.OnAltClick -= CancelPreparingMinionSkills;
    }

    public void Clear()
    {
        foreach (var group in _groups.Values)
            _panel.RemoveSkill(group.Representative);

        _groups.Clear();
        _byMinion.Clear();
        _skillRefCount.Clear();
    }
    
    private void PruneDestroyed()
    {
        var dead = _byMinion.Keys.Where(m => m == null).ToList();
        foreach (var m in dead) UnregisterMinion(m);
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
        PruneDestroyed();
        
        foreach (var skill in group.Instances.ToList())
        {
            if (skill == null || skill.Hero == null || skill.Hero.IsDead) continue;
            skill.Hero.GetComponent<SkillManager>()?.SelectAndPrepareSkill(skill, group);
        }
    }
    
    private void CancelPreparingMinionSkills()
    {
        foreach (var group in _groups.Values)
        {
            foreach (var skill in group.Instances)
            {
                if (skill != null && skill.IsTargeting)
                    skill.TryCancel();
            }
        }
    }
}