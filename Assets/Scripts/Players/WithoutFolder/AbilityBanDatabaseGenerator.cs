#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;

public static class AbilityBanDatabaseGenerator
{
    private const string AssetPath = "Assets/Resources/AbilityBanDatabase.asset";

    [MenuItem("Tools/Generate/Ability Ban Database")]
    public static void Generate()
    {
        var skillBaseType = typeof(Skill);
        var allSkills = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(asm =>
            {
                try { return asm.GetTypes(); }
                catch { return new Type[0]; }
            })
            .Where(t => skillBaseType.IsAssignableFrom(t) && !t.IsAbstract && !t.IsGenericType)
            .Select(t => t.Name)
            .OrderBy(n => n)
            .ToList();

        var db = AssetDatabase.LoadAssetAtPath<AbilityBanDatabase>(AssetPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<AbilityBanDatabase>();
            AssetDatabase.CreateAsset(db, AssetPath);
        }

        db.abilityNames = allSkills;
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
    }
}
#endif