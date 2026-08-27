using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public struct TalentSnapshotEntry
{
    public int group;
    public int row;
    public string name;
    public int lvl;
}

[Serializable]
public struct AttributeSnapshotEntry
{
    public string name;
    public int points;
}

[Serializable]
public struct HeroProgressSnapshot
{
    public int level;
    public int experience;
    public int experienceForNextLevel;
    public int talentPoints;
    public TalentSnapshotEntry[] talents;
    public AttributeSnapshotEntry[] attributes;
}

public static class HeroProgressSnapshotBuilder
{
    public static HeroProgressSnapshot Build(HeroComponent hero, UIMenuMainAttributesPanel attributesPanel)
    {
        var talents = new List<TalentSnapshotEntry>();
        foreach (var group in hero.TalentManager.TalentsGroups)
        foreach (var row in group.TalentRows)
        foreach (var talent in row.Talents)
        {
            if (!talent.Data.IsOpen) continue;
            talents.Add(new TalentSnapshotEntry
            {
                group = group.ID,
                row = talent.Data.Row,
                name = talent.Data.Name,
                lvl = talent.Data.Level
            });
        }

        var attributes = new List<AttributeSnapshotEntry>();
        var attrSystem = attributesPanel.AttributeSystem;
        if (attrSystem != null)
        {
            foreach (var attribute in attrSystem.Attributes.Values)
                attributes.Add(new AttributeSnapshotEntry { name = attribute.Name, points = attribute.Modifiers.Count });
        }

        return new HeroProgressSnapshot
        {
            level = LevelCharacterManager.Instance.GetCurrentLevel(),
            experience = LevelCharacterManager.Instance.GetCurrentExperience(),
            experienceForNextLevel = LevelCharacterManager.Instance.GetExperienceForNextLevel(),
            talentPoints = hero.TalentManager.Points,
            talents = talents.ToArray(),
            attributes = attributes.ToArray()
        };
    }
}

public static class HeroProgressSnapshotApplier
{
    public static void ApplyLevel(HeroComponent hero, HeroProgressSnapshot snapshot)
    {
        LevelCharacterManager.Instance.ApplyLoadedLevelData(hero, snapshot.level, snapshot.experience);
        hero.LVL.ApplyLoadedLevelLocal(snapshot.level, snapshot.experience, snapshot.experienceForNextLevel);
    }

    public static void ApplyTalentsAndAttributes(HeroComponent hero, HeroProgressSnapshot snapshot)
    {
        foreach (var group in hero.TalentManager.TalentsGroups)
        foreach (var row in group.TalentRows)
        foreach (var talent in row.Talents)
        {
            talent.Data.SetOpen(false);
            talent.Exit();
        }

        foreach (var entry in snapshot.talents ?? Array.Empty<TalentSnapshotEntry>())
        {
            var group = hero.TalentManager.TalentsGroups.FirstOrDefault(g => g.ID == entry.group);
            var talent = group?.TalentRows[entry.row].Talents.FirstOrDefault(t => t.Data.Name == entry.name);
            if (talent == null) continue;

            talent.Data.SetOpen(true);
            talent.Data.SetLevel(entry.lvl);
            talent.Enter();
        }

        hero.TalentManager.SetPoints(snapshot.talentPoints);

        foreach (var attribute in hero.AttributeSystem.Attributes.Values)
        {
            var entry = (snapshot.attributes ?? Array.Empty<AttributeSnapshotEntry>())
                .FirstOrDefault(a => a.name == attribute.Name);

            attribute.RemoveBySource("AttributePoint");
            for (int i = 0; i < entry.points; i++)
                attribute.AddModifier(new AttributeModifier(1, ModifierType.Flat, source: "AttributePoint"));
        }
        
        hero.AttributeSystem.RaiseAttributesReloaded();
    }
}