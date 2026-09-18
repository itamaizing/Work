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
        Debug.Log($"[MinionPanel] InsertIntoGroup: skill={skill.GetInstanceID()} ({skill.GetType().Name}) key={key}");

        if (!_groups.TryGetValue(key, out var group))
        {
            group = new MinionSkillGroup { GroupKey = key };
            _groups[key] = group;
            Debug.Log($"[MinionPanel] Created NEW group for key={key}");
        }

        if (group.Instances.Contains(skill))
        {
            Debug.Log($"[MinionPanel] skill={skill.GetInstanceID()} already in group key={key}, skip");
            return;
        }

        bool isNewGroup = group.Instances.Count == 0;
        group.Instances.Add(skill);
        _skillGroupKey[skill] = key;

        Debug.Log(
            $"[MinionPanel] Added skill={skill.GetInstanceID()} to group key={key}, group now has {group.Instances.Count} instances");

        if (isNewGroup)
            AddGroupIcon(group);
    }

    private void RegroupSkill(Skill skill)
    {
        Debug.Log($"[MinionPanel] RegroupSkill called for skill={skill.GetInstanceID()}");

        if (!_skillGroupKey.TryGetValue(skill, out var oldKey))
        {
            Debug.LogWarning(
                $"[MinionPanel] RegroupSkill: skill={skill.GetInstanceID()} has NO oldKey recorded, aborting");
            return;
        }

        var newKey = skill.GroupKey;
        Debug.Log($"[MinionPanel] RegroupSkill: oldKey={oldKey}, newKey={newKey}");

        if (Equals(oldKey, newKey))
        {
            Debug.Log($"[MinionPanel] RegroupSkill: keys equal, no-op");
            return;
        }

        if (_groups.TryGetValue(oldKey, out var oldGroup))
        {
            bool wasRepresentative = oldGroup.Representative == skill;
            oldGroup.Instances.Remove(skill);
            Debug.Log(
                $"[MinionPanel] Removed skill={skill.GetInstanceID()} from oldGroup key={oldKey}, remaining={oldGroup.Instances.Count}, wasRepresentative={wasRepresentative}");

            if (oldGroup.Instances.Count == 0)
            {
                _panel.RemoveSkill(skill);
                _groups.Remove(oldKey);
                Debug.Log($"[MinionPanel] oldGroup key={oldKey} now empty, removed group + icon");
            }
            else if (wasRepresentative)
            {
                _panel.GetIcon(skill)?.Rebind(oldGroup.Representative);
                Debug.Log(
                    $"[MinionPanel] Rebound icon to new representative={oldGroup.Representative?.GetInstanceID()}");
            }
        }
        else
        {
            Debug.LogWarning($"[MinionPanel] RegroupSkill: oldKey={oldKey} not found in _groups!");
        }

        InsertIntoGroup(skill, newKey);
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

    private void AddGroupIcon(MinionSkillGroup group)
    {
        _panel.AddSkill(group.Representative);
        var icon = _panel.GetIcon(group.Representative);
        if (icon != null)
            icon.ClickOverride = _ => { IssueGroupOrder(group); return true; };
    }

    private void IssueGroupOrder(MinionSkillGroup group)
    { 
        Debug.Log($"[MinionPanel] IssueGroupOrder for key={group.GroupKey}, {group.Instances.Count} instances: [{string.Join(", ", group.Instances.Select(s => s?.GetInstanceID().ToString() ?? "null"))}]");

        foreach (var skill in group.Instances.ToList())
        {
            if (skill == null || skill.Hero == null || skill.Hero.IsDead) continue;
            Debug.Log($"[MinionPanel] IssueGroupOrder -> SelectAndPrepareSkill on skill={skill.GetInstanceID()}");
            skill.Hero.GetComponent<SkillManager>()?.SelectAndPrepareSkill(skill);
        }
    }
    
    private void CancelPreparingMinionSkills()
    {
        foreach (var group in _groups.Values)
        {
            foreach (var skill in group.Instances)
            {
                if (skill != null && skill.IsPreparing)
                    skill.TryCancel();
            }
        }
    }
}