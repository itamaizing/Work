using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class TalentSchemaExporter
{
    [MenuItem("Tools/Talents/Export Schema For Selected Hero")]
    public static void ExportSelected()
    {
        var hero = Selection.activeGameObject?.GetComponent<HeroComponent>();
        if (hero == null)
        {
            Debug.LogError("Select a HeroComponent prefab/instance first.");
            return;
        }
        Export(hero);
    }

    [MenuItem("Tools/Talents/Export Schema For All Heroes In Project")]
    public static void ExportAll()
    {
        var guids = AssetDatabase.FindAssets("t:Prefab");
        int exported = 0;

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var hero = prefab != null ? prefab.GetComponent<HeroComponent>() : null;
            if (hero == null) continue;

            Export(hero);
            exported++;
        }

        Debug.Log($"[TalentSchemaExporter] Exported {exported} hero schema(s).");
    }

    private static void Export(HeroComponent hero)
    {
        var talentManager = hero.TalentManager;
        if (talentManager == null)
        {
            Debug.LogWarning($"{hero.name}: no TalentManager assigned, skipped.");
            return;
        }
        
        var talentsField = typeof(TalentSystem).GetField("_talents",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var groups = talentsField.GetValue(talentManager) as System.Collections.Generic.List<TalentsGroup>;
        if (groups == null) { Debug.LogWarning($"{hero.name}: talent groups list is null."); return; }

        var sb = new StringBuilder();
        sb.Append("{\n");
        sb.Append($"  \"hero\": \"{hero.Data.Name}\",\n");
        sb.Append("  \"talents\": {\n");

        var talentEntries = new System.Collections.Generic.List<string>();

        foreach (var group in groups)
        {
            for (int rowIndex = 0; rowIndex < group.TalentRows.Count; rowIndex++)
            {
                foreach (var talent in group.TalentRows[rowIndex].Talents)
                {
                    if (talent == null) continue;

                    string name = talent.GetType().Name;
                    int maxLvl = talent.Data.MaxLvl > 0 ? talent.Data.MaxLvl : 1;

                    string conditionJson = ExportCondition(talent.OpenCondition);

                    talentEntries.Add(
                        $"    \"{name}\": {{ \"group\": {group.ID}, \"row\": {rowIndex}, \"maxLvl\": {maxLvl}, \"condition\": {conditionJson} }}");
                }
            }
        }
        sb.Append(string.Join(",\n", talentEntries));
        sb.Append("\n  },\n");

        sb.Append("  \"rows\": {\n");
        var rowEntries = new System.Collections.Generic.List<string>();

        foreach (var group in groups)
        {
            for (int rowIndex = 0; rowIndex < group.TalentRows.Count; rowIndex++)
            {
                var conditions = group.TalentRows[rowIndex].OpenConditions;
                if (conditions == null || conditions.Count == 0) continue;

                var conditionJsons = conditions.Where(c => c != null).Select(ExportRowCondition);
                rowEntries.Add($"    \"{group.ID}_{rowIndex}\": [{string.Join(", ", conditionJsons)}]");
            }
        }
        sb.Append(string.Join(",\n", rowEntries));
        sb.Append("\n  }\n");
        sb.Append("}\n");

        string dir = "Assets/StreamingAssets/talent_schemas";
        Directory.CreateDirectory(dir);
        string filePath = $"{dir}/{hero.Data.Name}.json";
        File.WriteAllText(filePath, sb.ToString());
        Debug.Log($"[TalentSchemaExporter] Wrote {filePath}");
    }

    private static string ExportCondition(OpenCondition condition)
    {
        switch (condition)
        {
            case SpecificTalentOpenCondition specific:
                var names = specific.TalentsNeededToOpen?.Select(n => $"\"{n}\"") ?? Enumerable.Empty<string>();
                return $"{{ \"type\": \"specific\", \"talents\": [{string.Join(", ", names)}] }}";

            case CountTalentsOpenCondition countT:
                return $"{{ \"type\": \"countTalents\", \"count\": {countT.count} }}";

            case CountPointsCondition countP:
                return $"{{ \"type\": \"countPoints\", \"count\": {countP.count} }}";

            case EmptyCondition:
            case null:
            default:
                return "{ \"type\": \"none\" }";
        }
    }

    private static string ExportRowCondition(RowOpenCondition condition)
    {
        switch (condition)
        {
            case SpendPointsInBranchCondition spendBranch:
                return $"{{ \"type\": \"spendPointsInBranch\", \"count\": {spendBranch.count} }}";

            case SpendPointsInPreviousRowCondition spendPrev:
                return $"{{ \"type\": \"spendPointsInPreviousRow\", \"count\": {spendPrev.count} }}";

            case OpenTalentsInBranchCondition openBranch:
                return $"{{ \"type\": \"openTalentsInBranch\", \"count\": {openBranch.count} }}";

            case SpecificTalentOpenRowCondition specificRow:
                var names = specificRow.TalentsNeededToOpen?.Select(n => $"\"{n}\"") ?? Enumerable.Empty<string>();
                return $"{{ \"type\": \"specificTalent\", \"talents\": [{string.Join(", ", names)}] }}";

            default:
                return "{ \"type\": \"none\" }";
        }
    }
}